using System.Security.Claims;
using Cbo.API.Authorization;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cbo.API.Services;

public interface IRoundService
{
    Task<Result<GetRoundDto>> CreateRoundAsync(Guid tournamentId, Guid matchId, CreateRoundWithAnswersDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<Result<GetRoundDto>> UpdateRoundAsync(Guid tournamentId, Guid matchId, int roundNumber, CreateRoundWithAnswersDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<Result> DeleteRoundAsync(Guid tournamentId, Guid matchId, int roundNumber, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

public class RoundService(
    ITournamentRepository tournamentRepository,
    IMatchRepository matchRepository,
    IRoundRepository roundRepository,
    ITopicRepository topicRepository,
    ITournamentTopicRepository tournamentTopicRepository,
    ITournamentParticipantsRepository participantsRepository,
    IAuthorizationService authorizationService,
    CboDbContext dbContext) : IRoundService
{
    private readonly ITournamentRepository _tournamentRepository = tournamentRepository;
    private readonly IMatchRepository _matchRepository = matchRepository;
    private readonly IRoundRepository _roundRepository = roundRepository;
    private readonly ITopicRepository _topicRepository = topicRepository;
    private readonly ITournamentTopicRepository _tournamentTopicRepository = tournamentTopicRepository;
    private readonly ITournamentParticipantsRepository _participantsRepository = participantsRepository;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly CboDbContext _dbContext = dbContext;

    public async Task<Result<GetRoundDto>> CreateRoundAsync(Guid tournamentId, Guid matchId, CreateRoundWithAnswersDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        Result matchAccess = await EnsureMatchAccessAsync(tournamentId, matchId, user, cancellationToken);
        if (matchAccess.IsFailure)
            return matchAccess.As<GetRoundDto>();

        if (dto.NumberInMatch < 1 || dto.NumberInMatch > DefaultSettings.RoundsPerMatch)
            return Result.Invalid<GetRoundDto>(RoundErrors.NumberOutOfRange());

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, dto.NumberInMatch, cancellationToken);
        if (existingRound is not null)
            return Result.Conflict<GetRoundDto>(RoundErrors.AlreadyExists(dto.NumberInMatch));

        Topic? topic = await _topicRepository.GetByIdAsync(dto.TopicId, cancellationToken);
        if (topic is null)
            return Result.Invalid<GetRoundDto>(RoundErrors.TopicNotFound());

        Result answersValidation = ValidateRoundAnswers(dto.Answers, dto.IsOverrideMode);
        if (answersValidation.IsFailure)
            return answersValidation.As<GetRoundDto>();

        Round round = dto.ToNewRound(matchId);
        foreach (CreateRoundAnswerDto answerDto in dto.Answers)
        {
            round.RoundAnswers.Add(answerDto.ToNewRoundAnswer(Guid.Empty));
        }

        // Disposing an uncommitted transaction rolls it back; unexpected exceptions propagate to the global handler.
        await using (IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            await _roundRepository.CreateAsync(round, cancellationToken);
            await RecalculateMatchScoresAsync(matchId, cancellationToken);
            await CommitAsync(transaction);
        }

        Result<GetRoundDto> loadResult = await LoadRoundDtoAsync(round.Id, tournamentId, cancellationToken);
        if (loadResult.IsFailure)
            return loadResult;

        string location = $"/api/tournaments/{tournamentId}/matches/{matchId}/rounds/{round.NumberInMatch}";
        return Result.Created(loadResult.Value, location);
    }

    public async Task<Result<GetRoundDto>> UpdateRoundAsync(Guid tournamentId, Guid matchId, int roundNumber, CreateRoundWithAnswersDto dto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        Result matchAccess = await EnsureMatchAccessAsync(tournamentId, matchId, user, cancellationToken);
        if (matchAccess.IsFailure)
            return matchAccess.As<GetRoundDto>();

        if (dto.NumberInMatch != roundNumber)
            return Result.Invalid<GetRoundDto>(RoundErrors.NumberMismatch());

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber, cancellationToken);
        if (existingRound is null)
            return Result.NotFound<GetRoundDto>(RoundErrors.NotFound(roundNumber));

        if (existingRound.TopicId != dto.TopicId || existingRound.IsOverrideMode != dto.IsOverrideMode)
            return Result.Conflict<GetRoundDto>(RoundErrors.Immutable());

        Result answersValidation = ValidateRoundAnswers(dto.Answers, dto.IsOverrideMode);
        if (answersValidation.IsFailure)
            return answersValidation.As<GetRoundDto>();

        List<RoundAnswer> newAnswers = dto.Answers
            .Select(a => a.ToNewRoundAnswer(existingRound.Id))
            .ToList();

        await using (IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            await _roundRepository.DeleteAnswersByRoundIdAsync(existingRound.Id, cancellationToken);
            await _roundRepository.CreateAnswersAsync(newAnswers, cancellationToken);
            await RecalculateMatchScoresAsync(matchId, cancellationToken);
            await CommitAsync(transaction);
        }

        return await LoadRoundDtoAsync(existingRound.Id, tournamentId, cancellationToken);
    }

    public async Task<Result> DeleteRoundAsync(Guid tournamentId, Guid matchId, int roundNumber, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        Result matchAccess = await EnsureMatchAccessAsync(tournamentId, matchId, user, cancellationToken);
        if (matchAccess.IsFailure)
            return matchAccess;

        Round? existingRound = await _roundRepository.GetByMatchIdAndNumberAsync(matchId, roundNumber, cancellationToken);
        if (existingRound is null)
            return Result.NotFound(RoundErrors.NotFound(roundNumber));

        await using (IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            await _roundRepository.DeleteAsync(existingRound.Id, cancellationToken);
            await RecalculateMatchScoresAsync(matchId, cancellationToken);
            await CommitAsync(transaction);
        }

        return Result.NoContent();
    }

    /// <summary>
    /// Commits without the request's cancellation token. Every write has already succeeded at this point;
    /// cancelling an in-flight COMMIT would leave the client unable to tell whether it was applied.
    /// Cancellation before this point simply rolls the transaction back on dispose.
    /// </summary>
    private static Task CommitAsync(IDbContextTransaction transaction) => transaction.CommitAsync(CancellationToken.None);

    /// <summary>
    /// Verifies that the tournament exists, the caller may manage its rounds, and the match belongs to it.
    /// Every failure is <see cref="ResultStatus.NotFound"/> without details so that unauthorized callers cannot probe for existence.
    /// </summary>
    private async Task<Result> EnsureMatchAccessAsync(Guid tournamentId, Guid matchId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId, cancellationToken);
        if (tournament is null)
            return Result.NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(user, tournament, TournamentOperations.ManageRounds);
        if (!authResult.Succeeded)
            return Result.NotFound();

        Match? match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is null || match.TournamentId != tournamentId)
            return Result.NotFound();

        return Result.Ok();
    }

    /// <summary>
    /// Reloads a saved round with the details <c>GetRoundDto</c> needs. Failures here mean the write
    /// succeeded but the read-back did not, which is an infrastructure problem, hence <see cref="ResultStatus.Unexpected"/>.
    /// These errors carry no <see cref="Error.Code"/>: there is nothing a client can do about them.
    /// </summary>
    private async Task<Result<GetRoundDto>> LoadRoundDtoAsync(Guid roundId, Guid tournamentId, CancellationToken cancellationToken)
    {
        Round? round = await _roundRepository.GetByIdWithDetailsAsync(roundId, cancellationToken);
        if (round is null)
            return Result.Unexpected<GetRoundDto>(new Error("Failed to retrieve the saved round."));

        List<TournamentTopic> tournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId, cancellationToken);
        TournamentTopic? tournamentTopic = tournamentTopics.FirstOrDefault(tt => tt.TopicId == round.TopicId);
        if (tournamentTopic is null)
            return Result.Unexpected<GetRoundDto>(new Error("Failed to find the tournament topic for the round."));

        string ownerUsername = tournamentTopic.TournamentParticipant.ApplicationUser?.UserName ?? string.Empty;
        return Result.Ok(round.ToGetDto(tournamentTopic.PriorityIndex, ownerUsername));
    }

    private static Result ValidateRoundAnswers(List<CreateRoundAnswerDto> answers, bool isOverrideMode)
    {
        if (isOverrideMode)
        {
            foreach (CreateRoundAnswerDto answer in answers)
            {
                if (answer.IsAnswerAccepted.HasValue)
                    return Result.Invalid(RoundErrors.AnswerAcceptedNotAllowed());

                if (!answer.OverrideCost.HasValue)
                    return Result.Invalid(RoundErrors.OverrideCostRequired());
            }
        }
        else
        {
            foreach (CreateRoundAnswerDto answer in answers)
            {
                if (!answer.IsAnswerAccepted.HasValue)
                    return Result.Invalid(RoundErrors.AnswerAcceptedRequired());

                if (answer.OverrideCost.HasValue)
                    return Result.Invalid(RoundErrors.OverrideCostNotAllowed());
            }

            var positiveAnswersByQuestion = answers
                .Where(a => a.IsAnswerAccepted == true)
                .GroupBy(a => a.QuestionId)
                .Where(g => g.Count() > 1)
                .ToList();

            if (positiveAnswersByQuestion.Count > 0)
                return Result.Invalid(RoundErrors.MultipleCorrectAnswers(positiveAnswersByQuestion.First().Key));
        }

        return Result.Ok();
    }

    private async Task RecalculateMatchScoresAsync(Guid matchId, CancellationToken cancellationToken)
    {
        Match? match = await _matchRepository.GetByIdWithScoreDataAsync(matchId, cancellationToken);
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

        await _matchRepository.UpdateMatchParticipantsAsync(match.MatchParticipants.ToList(), cancellationToken);

        await RecalculateTournamentScoresAsync(match.TournamentId, cancellationToken);
    }

    private async Task RecalculateTournamentScoresAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        List<TournamentParticipant> participants = await _participantsRepository
            .GetAllByTournamentIdWithMatchDataAsync(tournamentId, cancellationToken);

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

        await _participantsRepository.UpdateParticipantsAsync(participants, cancellationToken);
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
