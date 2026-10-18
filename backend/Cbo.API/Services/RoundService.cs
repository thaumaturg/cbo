using System.Security.Claims;
using Cbo.API.Authorization;
using Cbo.API.Data;
using Cbo.API.Data.Configuration;
using Cbo.API.Mappings;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Cbo.API.Services;

public interface IRoundService
{
    Task<Result<GetRoundDto>> GetRoundAsync(Guid tournamentId, Guid matchId, int roundNumber);
    Task<Result<GetRoundDto>> CreateRoundAsync(Guid tournamentId, Guid matchId, CreateRoundWithAnswersDto createRoundDto);
    Task<Result<GetRoundDto>> UpdateRoundAsync(Guid tournamentId, Guid matchId, int roundNumber, CreateRoundWithAnswersDto updateRoundDto);
    Task<Result> DeleteRoundAsync(Guid tournamentId, Guid matchId, int roundNumber);
}

public class RoundService(
    CboDbContext dbContext,
    ITournamentRepository tournamentRepository,
    IMatchRepository matchRepository,
    IRoundRepository roundRepository,
    ITopicRepository topicRepository,
    ITournamentTopicRepository tournamentTopicRepository,
    ITournamentParticipantsRepository participantsRepository,
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor) : IRoundService
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string ManageRoundsForbiddenMessage = "You don't have permission to manage rounds in this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentRepository _tournamentRepository = tournamentRepository;
    private readonly IMatchRepository _matchRepository = matchRepository;
    private readonly IRoundRepository _roundRepository = roundRepository;
    private readonly ITopicRepository _topicRepository = topicRepository;
    private readonly ITournamentTopicRepository _tournamentTopicRepository = tournamentTopicRepository;
    private readonly ITournamentParticipantsRepository _participantsRepository = participantsRepository;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<Result<GetRoundDto>> GetRoundAsync(Guid tournamentId, Guid matchId, int roundNumber)
    {
        Result<Match> access = await LoadMatchWithAccessAsync(tournamentId, matchId, TournamentOperations.Read, ReadForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        Round? round = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber);
        if (round is null)
            return RoundNotFound(roundNumber);

        return await LoadRoundDtoAsync(tournamentId, round.Id);
    }

    public async Task<Result<GetRoundDto>> CreateRoundAsync(Guid tournamentId, Guid matchId, CreateRoundWithAnswersDto createRoundDto)
    {
        Result<Match> access = await LoadMatchWithAccessAsync(tournamentId, matchId, TournamentOperations.ManageRounds, ManageRoundsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        Result validation = await ValidateRoundRequestAsync(tournamentId, access.Value, createRoundDto);
        if (validation.IsFailure)
            return validation.Failure;

        int roundNumber = createRoundDto.NumberInMatch;
        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber);
        if (existingRound is not null)
            return RoundAlreadyExists(roundNumber);

        Round round = createRoundDto.ToNewRound(matchId);
        foreach (CreateRoundAnswerDto answerDto in createRoundDto.Answers)
        {
            round.RoundAnswers.Add(answerDto.ToNewRoundAnswer(Guid.Empty));
        }

        try
        {
            await ExecuteInTransactionAsync(async () =>
            {
                await _roundRepository.CreateAsync(round);
                await RecalculateMatchScoresAsync(matchId);
            });
        }
        catch (DbUpdateException exception) when (IsRoundNumberUniqueViolation(exception))
        {
            // Lost a race with a simultaneous create of the same round
            return RoundAlreadyExists(roundNumber);
        }

        return await LoadRoundDtoAsync(tournamentId, round.Id);
    }

    public async Task<Result<GetRoundDto>> UpdateRoundAsync(Guid tournamentId, Guid matchId, int roundNumber, CreateRoundWithAnswersDto updateRoundDto)
    {
        Result<Match> access = await LoadMatchWithAccessAsync(tournamentId, matchId, TournamentOperations.ManageRounds, ManageRoundsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber);
        if (existingRound is null)
            return RoundNotFound(roundNumber);

        if (updateRoundDto.NumberInMatch != roundNumber)
            return InvalidField("numberInMatch", "Round number in URL must match round number in body.");

        Result validation = await ValidateRoundRequestAsync(tournamentId, access.Value, updateRoundDto);
        if (validation.IsFailure)
            return validation.Failure;

        if (existingRound.TopicId != updateRoundDto.TopicId ||
            existingRound.IsOverrideMode != updateRoundDto.IsOverrideMode)
            return Failure.Conflict("The topic, number and scoring mode of an existing round can't be changed. Delete the round and create a new one.");

        List<RoundAnswer> newAnswers = updateRoundDto.Answers
            .Select(a => a.ToNewRoundAnswer(existingRound.Id))
            .ToList();

        await ExecuteInTransactionAsync(async () =>
        {
            await _roundRepository.DeleteAnswersByRoundIdAsync(existingRound.Id);
            await _roundRepository.CreateAnswersAsync(newAnswers);
            await RecalculateMatchScoresAsync(matchId);
        });

        return await LoadRoundDtoAsync(tournamentId, existingRound.Id);
    }

    public async Task<Result> DeleteRoundAsync(Guid tournamentId, Guid matchId, int roundNumber)
    {
        Result<Match> access = await LoadMatchWithAccessAsync(tournamentId, matchId, TournamentOperations.ManageRounds, ManageRoundsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber);
        if (existingRound is null)
            return RoundNotFound(roundNumber);

        await ExecuteInTransactionAsync(async () =>
        {
            await _roundRepository.DeleteAsync(existingRound.Id);
            await RecalculateMatchScoresAsync(matchId);
        });

        return Result.Success();
    }

    /// <summary>
    /// Loads the tournament and the match (with its participants) from the URL and checks the current user's permission on the tournament.
    /// </summary>
    private async Task<Result<Match>> LoadMatchWithAccessAsync(
        Guid tournamentId,
        Guid matchId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return Failure.NotFound("Tournament not found.");

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), tournament, operation);
        if (!authResult.Succeeded)
            return Failure.Forbidden(forbiddenMessage);

        Match? match = await _matchRepository.GetByIdWithParticipantsAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return Failure.NotFound("Match not found in this tournament.");

        return match;
    }

    /// <summary>
    /// Checks everything that can be wrong with the round in the request body itself. All of it is <see cref="FailureKind.Invalid"/>.
    /// </summary>
    private async Task<Result> ValidateRoundRequestAsync(Guid tournamentId, Match match, CreateRoundWithAnswersDto roundDto)
    {
        if (roundDto.NumberInMatch < 1 || roundDto.NumberInMatch > DefaultSettings.RoundsPerMatch)
            return InvalidField("numberInMatch", $"Round number must be between 1 and {DefaultSettings.RoundsPerMatch}.");

        Topic? topic = await _topicRepository.GetByIdIncludeQuestionsAsync(roundDto.TopicId);
        if (topic is null)
            return InvalidField("topicId", "Topic not found.");

        List<TournamentTopic> tournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);
        if (!tournamentTopics.Any(tt => tt.TopicId == roundDto.TopicId))
            return InvalidField("topicId", "This topic is not part of this tournament.");

        Result answersValidation = ValidateRoundAnswers(roundDto.Answers, roundDto.IsOverrideMode);
        if (answersValidation.IsFailure)
            return answersValidation.Failure;

        HashSet<Guid> topicQuestionIds = topic.Questions.Select(q => q.Id).ToHashSet();
        CreateRoundAnswerDto? foreignQuestionAnswer = roundDto.Answers.FirstOrDefault(a => !topicQuestionIds.Contains(a.QuestionId));
        if (foreignQuestionAnswer is not null)
            return InvalidField("answers", $"Question {foreignQuestionAnswer.QuestionId} is not part of the round's topic.");

        HashSet<Guid> matchParticipantIds = match.MatchParticipants.Select(mp => mp.Id).ToHashSet();
        CreateRoundAnswerDto? foreignParticipantAnswer = roundDto.Answers.FirstOrDefault(a => !matchParticipantIds.Contains(a.MatchParticipantId));
        if (foreignParticipantAnswer is not null)
            return InvalidField("answers", $"Participant {foreignParticipantAnswer.MatchParticipantId} is not part of this match.");

        var duplicateAnswer = roundDto.Answers
            .GroupBy(a => new { a.QuestionId, a.MatchParticipantId })
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateAnswer is not null)
            return InvalidField("answers", $"Participant {duplicateAnswer.Key.MatchParticipantId} has more than one answer to question {duplicateAnswer.Key.QuestionId}.");

        return Result.Success();
    }

    private ClaimsPrincipal GetCurrentUser()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("No authenticated user in the current request. Round operations must only be called from [Authorize] endpoints.");

        return user;
    }

    private async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static bool IsRoundNumberUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: RoundConfiguration.MatchIdNumberInMatchIndexName,
        };

    /// <summary>
    /// Reloads a round that is known to exist and maps it. A missing round or tournament topic is a bug, so it throws.
    /// </summary>
    private async Task<GetRoundDto> LoadRoundDtoAsync(Guid tournamentId, Guid roundId)
    {
        Round round = await _roundRepository.GetByIdWithDetailsAsync(roundId)
            ?? throw new InvalidOperationException($"Round {roundId} could not be reloaded.");

        List<TournamentTopic> tournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);
        TournamentTopic tournamentTopic = tournamentTopics.FirstOrDefault(tt => tt.TopicId == round.TopicId)
            ?? throw new InvalidOperationException($"Topic {round.TopicId} of round {roundId} is not a topic of tournament {tournamentId}.");

        string ownerUsername = tournamentTopic.TournamentParticipant?.ApplicationUser?.UserName ?? string.Empty;

        return round.ToGetDto(tournamentTopic.PriorityIndex, ownerUsername);
    }

    private static Failure InvalidField(string field, string message) =>
        Failure.Invalid("Some of the round data is invalid.", new FieldError(field, message));

    private static Failure RoundNotFound(int roundNumber) =>
        Failure.NotFound($"Round {roundNumber} not found for this match.");

    private static Failure RoundAlreadyExists(int roundNumber) =>
        Failure.Conflict($"Round {roundNumber} already exists for this match. Edit it instead of creating it again.");

    private static Result ValidateRoundAnswers(List<CreateRoundAnswerDto> answers, bool isOverrideMode)
    {
        if (isOverrideMode)
        {
            foreach (CreateRoundAnswerDto answer in answers)
            {
                if (answer.IsAnswerAccepted.HasValue)
                    return InvalidField("answers", "In override mode, IsAnswerAccepted must be null for all answers.");

                if (!answer.OverrideCost.HasValue)
                    return InvalidField("answers", "In override mode, OverrideCost must be provided for all answers.");
            }
        }
        else
        {
            foreach (CreateRoundAnswerDto answer in answers)
            {
                if (!answer.IsAnswerAccepted.HasValue)
                    return InvalidField("answers", "In standard mode, IsAnswerAccepted must be provided for all answers.");

                if (answer.OverrideCost.HasValue)
                    return InvalidField("answers", "In standard mode, OverrideCost must be null for all answers.");
            }

            var positiveAnswersByQuestion = answers
                .Where(a => a.IsAnswerAccepted == true)
                .GroupBy(a => a.QuestionId)
                .Where(g => g.Count() > 1)
                .ToList();

            if (positiveAnswersByQuestion.Count > 0)
            {
                Guid questionId = positiveAnswersByQuestion.First().Key;
                return InvalidField("answers", $"Question {questionId} has multiple correct answers. Only one correct answer is allowed per question.");
            }
        }

        return Result.Success();
    }

    private async Task RecalculateMatchScoresAsync(Guid matchId)
    {
        Match? match = await _matchRepository.GetByIdWithScoreDataAsync(matchId);
        if (match is null)
            return;

        foreach (MatchParticipant participant in match.MatchParticipants)
        {
            int score = 0;
            foreach (RoundAnswer answer in participant.RoundAnswers)
            {
                if (answer.OverrideCost.HasValue)
                {
                    score += answer.OverrideCost.Value;
                }
                else if (answer.IsAnswerAccepted.HasValue)
                {
                    if (answer.IsAnswerAccepted.Value)
                        score += answer.Question.CostPositive;
                    else
                        score -= answer.Question.CostNegative;
                }
            }
            participant.ScoreSum = score;
        }

        if (match.Rounds.Count == DefaultSettings.RoundsPerMatch)
        {
            CalculatePoints(match.MatchParticipants.ToList());
        }
        else
        {
            foreach (MatchParticipant participant in match.MatchParticipants)
            {
                participant.PointsSum = null;
            }
        }

        await _matchRepository.UpdateMatchParticipantsAsync(match.MatchParticipants.ToList());

        await RecalculateTournamentScoresAsync(match.TournamentId);
    }

    private async Task RecalculateTournamentScoresAsync(Guid tournamentId)
    {
        List<TournamentParticipant> participants = await _participantsRepository
            .GetAllByTournamentIdWithMatchDataAsync(tournamentId);

        foreach (TournamentParticipant participant in participants)
        {
            int scoreSum = participant.MatchParticipants
                .Where(mp => mp.ScoreSum.HasValue)
                .Sum(mp => mp.ScoreSum!.Value);

            decimal pointsSum = participant.MatchParticipants
                .Where(mp => mp.PointsSum.HasValue)
                .Sum(mp => mp.PointsSum!.Value);

            participant.ScoreSum = scoreSum;
            participant.PointsSum = pointsSum;
        }

        await _participantsRepository.UpdateParticipantsAsync(participants);
    }

    private static void CalculatePoints(List<MatchParticipant> participants)
    {
        int count = participants.Count;
        if (count < DefaultSettings.ParticipantsPerMatchMin || count > DefaultSettings.ParticipantsPerMatchMax)
            return;

        var sorted = participants.OrderByDescending(p => p.ScoreSum ?? 0).ToList();
        decimal[] points = GetPointsDistribution(sorted);

        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].PointsSum = points[i];
        }
    }

    private static decimal[] GetPointsDistribution(List<MatchParticipant> sorted)
    {
        int count = sorted.Count;
        int[] scores = sorted.Select(p => p.ScoreSum ?? 0).ToArray();

        return count switch
        {
            2 => GetPointsFor2Players(scores),
            3 => GetPointsFor3Players(scores),
            4 => GetPointsFor4Players(scores),
            _ => []
        };
    }

    private static decimal[] GetPointsFor2Players(int[] scores)
    {
        if (scores[0] == scores[1])
            return [3m, 3m];

        return [4m, 2m];
    }

    private static decimal[] GetPointsFor3Players(int[] scores)
    {
        bool top2Same = scores[0] == scores[1];
        bool bottom2Same = scores[1] == scores[2];

        if (top2Same && bottom2Same)
            return [2m, 2m, 2m];

        if (top2Same)
            return [2.5m, 2.5m, 1m];

        if (bottom2Same)
            return [3m, 1.5m, 1.5m];

        return [3m, 2m, 1m];
    }

    // Points are split across tied places.
    // Base points: 1st = 3, 2nd = 2, 3rd = 1, 4th = 0.
    // Tied players share the sum of their places equally.
    private static decimal[] GetPointsFor4Players(int[] scores)
    {
        bool firstTiedWithSecond = scores[0] == scores[1];
        bool secondTiedWithThird = scores[1] == scores[2];
        bool thirdTiedWithFourth = scores[2] == scores[3];

        // All four tied: (3+2+1+0)/4 = 1.5 each
        if (firstTiedWithSecond && secondTiedWithThird && thirdTiedWithFourth)
            return [1.5m, 1.5m, 1.5m, 1.5m];

        if (firstTiedWithSecond && secondTiedWithThird && !thirdTiedWithFourth)
            return [2m, 2m, 2m, 0m];

        if (!firstTiedWithSecond && secondTiedWithThird && thirdTiedWithFourth)
            return [3m, 1m, 1m, 1m];

        if (firstTiedWithSecond && !secondTiedWithThird && thirdTiedWithFourth)
            return [2.5m, 2.5m, 0.5m, 0.5m];

        if (firstTiedWithSecond && !secondTiedWithThird && !thirdTiedWithFourth)
            return [2.5m, 2.5m, 1m, 0m];

        if (!firstTiedWithSecond && secondTiedWithThird && !thirdTiedWithFourth)
            return [3m, 1.5m, 1.5m, 0m];

        if (!firstTiedWithSecond && !secondTiedWithThird && thirdTiedWithFourth)
            return [3m, 2m, 0.5m, 0.5m];

        return [3m, 2m, 1m, 0m];
    }
}
