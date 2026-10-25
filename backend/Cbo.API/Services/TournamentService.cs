using Cbo.API.Authorization;
using Cbo.API.Data;
using Cbo.API.Data.Configuration;
using Cbo.API.Mappings;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cbo.API.Services;

public interface ITournamentService
{
    Task<List<GetTournamentDto>> GetTournamentsAsync();
    Task<Result<GetTournamentDto>> GetTournamentAsync(Guid tournamentId);
    Task<Result<GetTournamentDto>> CreateTournamentAsync(CreateTournamentDto createTournamentDto);
    Task<Result<GetTournamentDto>> UpdateTournamentAsync(Guid tournamentId, UpdateTournamentDto updateTournamentDto);
    Task<Result> DeleteTournamentAsync(Guid tournamentId);
    Task<Result<GetTournamentDto>> AdvanceStageAsync(Guid tournamentId, UpdateTournamentStageDto advanceStageDto);
}

public class TournamentService(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    ITournamentRepository tournamentRepository,
    ITournamentParticipantsRepository participantsRepository,
    ITournamentTopicRepository tournamentTopicRepository,
    IMatchRepository matchRepository,
    IMatchGenerationService matchGenerationService,
    ICurrentUserService currentUserService) : ITournamentService
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string UpdateForbiddenMessage = "You don't have permission to edit this tournament.";
    private const string DeleteForbiddenMessage = "You don't have permission to delete this tournament.";
    private const string AdvanceStageForbiddenMessage = "You don't have permission to start this tournament.";
    private const string InvalidTournamentMessage = "Some of the tournament data is invalid.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ITournamentRepository _tournamentRepository = tournamentRepository;
    private readonly ITournamentParticipantsRepository _participantsRepository = participantsRepository;
    private readonly ITournamentTopicRepository _tournamentTopicRepository = tournamentTopicRepository;
    private readonly IMatchRepository _matchRepository = matchRepository;
    private readonly IMatchGenerationService _matchGenerationService = matchGenerationService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<List<GetTournamentDto>> GetTournamentsAsync()
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        List<Tournament> tournaments = await _tournamentRepository.GetAllByUserIdAsync(currentUserId);

        return tournaments
            .Select(t => t.ToGetDto(t.TournamentParticipants.FirstOrDefault()?.Role))
            .ToList();
    }

    public async Task<Result<GetTournamentDto>> GetTournamentAsync(Guid tournamentId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.Read, ReadForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        return await ToGetDtoForCurrentUserAsync(access.Value);
    }

    public async Task<Result<GetTournamentDto>> CreateTournamentAsync(CreateTournamentDto createTournamentDto)
    {
        TournamentSettings settings = ResolveSettings(
            createTournamentDto.PlayersPerTournament,
            createTournamentDto.TopicsPerParticipantMax,
            createTournamentDto.TopicsPerParticipantMin);

        List<FieldError> fieldErrors = ValidateSettings(settings);
        if (fieldErrors.Count > 0)
            return Failure.Invalid(InvalidTournamentMessage, fieldErrors);

        Guid creatorId = _currentUserService.GetRequiredCurrentUserId();

        Tournament tournament = createTournamentDto.ToNewTournament(
            TournamentStage.Preparations,
            settings.PlayersPerTournament,
            settings.TopicsPerParticipantMax,
            settings.TopicsPerParticipantMin);

        tournament.TournamentParticipants.Add(new TournamentParticipant
        {
            Role = TournamentParticipantRole.Creator,
            PointsSum = null,
            ApplicationUserId = creatorId,
        });

        tournament = await _tournamentRepository.CreateAsync(tournament);

        return tournament.ToGetDto(TournamentParticipantRole.Creator);
    }

    public async Task<Result<GetTournamentDto>> UpdateTournamentAsync(Guid tournamentId, UpdateTournamentDto updateTournamentDto)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.Update, UpdateForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        TournamentSettings settings = ResolveSettings(
            updateTournamentDto.PlayersPerTournament,
            updateTournamentDto.TopicsPerParticipantMax,
            updateTournamentDto.TopicsPerParticipantMin);

        List<FieldError> fieldErrors = ValidateSettings(settings);
        if (fieldErrors.Count > 0)
            return Failure.Invalid(InvalidTournamentMessage, fieldErrors);

        if (access.Value.CurrentStage != TournamentStage.Preparations)
            return Failure.Conflict("A tournament can only be edited during the Preparations stage.");

        var updateParameters = new UpdateTournamentParameters
        {
            Title = updateTournamentDto.Title,
            Description = updateTournamentDto.Description,
            PlayersPerTournament = settings.PlayersPerTournament,
            TopicsPerParticipantMax = settings.TopicsPerParticipantMax,
            TopicsPerParticipantMin = settings.TopicsPerParticipantMin
        };

        Tournament? updatedTournament = await _tournamentRepository.UpdateAsync(tournamentId, updateParameters);
        if (updatedTournament is null)
            return TournamentNotFound();

        return await ToGetDtoForCurrentUserAsync(updatedTournament);
    }

    public async Task<Result> DeleteTournamentAsync(Guid tournamentId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.Delete, DeleteForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        Tournament? deletedTournament = null;
        await _dbContext.Database.ExecuteInTransactionAsync(async () =>
        {
            deletedTournament = await _tournamentRepository.DeleteAsync(tournamentId);
        });

        if (deletedTournament is null)
            return TournamentNotFound();

        return Result.Success();
    }

    public async Task<Result<GetTournamentDto>> AdvanceStageAsync(Guid tournamentId, UpdateTournamentStageDto advanceStageDto)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.AdvanceStage, AdvanceStageForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;

        if (advanceStageDto.Stage != TournamentStage.Qualifications)
            return Failure.Invalid(
                "Some of the stage data is invalid.",
                new FieldError("stage", "Only advancing to the Qualifications stage is currently supported."));

        if (tournament.CurrentStage != TournamentStage.Preparations)
            return AlreadyStarted();

        List<TournamentParticipant> participants = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId);

        List<TournamentParticipant> players = participants
            .Where(p => p.Role == TournamentParticipantRole.Player)
            .OrderBy(p => p.Seed ?? int.MaxValue)
            .ThenBy(p => p.Id)
            .ToList();

        if (players.Count != tournament.PlayersPerTournament)
            return Failure.Conflict($"The tournament needs exactly {tournament.PlayersPerTournament} players to start. It has {players.Count}.");

        Result topicCountCheck = await CheckTopicCountsAsync(tournament, participants);
        if (topicCountCheck.IsFailure)
            return topicCountCheck.Failure;

        List<Match> matches = _matchGenerationService.GenerateQualificationMatches(tournament, players);

        Tournament? updatedTournament = null;
        try
        {
            await _dbContext.Database.ExecuteInTransactionAsync(async () =>
            {
                await _matchRepository.CreateBulkAsync(matches);
                updatedTournament = await _tournamentRepository.UpdateStageAsync(tournamentId, advanceStageDto.Stage);
            });
        }
        catch (DbUpdateException exception) when (IsMatchNumberUniqueViolation(exception))
        {
            // Lost a race with a simultaneous advance of the same tournament
            return AlreadyStarted();
        }

        if (updatedTournament is null)
            return TournamentNotFound();

        return await ToGetDtoForCurrentUserAsync(updatedTournament);
    }

    /// <summary>
    /// Every player needs between the minimum and the maximum number of topics; other participants at most the maximum.
    /// </summary>
    private async Task<Result> CheckTopicCountsAsync(Tournament tournament, List<TournamentParticipant> participants)
    {
        List<TournamentTopic> topics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournament.Id);

        foreach (TournamentParticipant participant in participants)
        {
            int topicCount = topics.Count(t => t.TournamentParticipantId == participant.Id);
            string name = participant.ApplicationUser?.UserName ?? participant.Id.ToString();

            if (participant.Role == TournamentParticipantRole.Player && topicCount < tournament.TopicsPerParticipantMin)
                return Failure.Conflict($"Player '{name}' has {topicCount} topics, but needs at least {tournament.TopicsPerParticipantMin}.");

            if (topicCount > tournament.TopicsPerParticipantMax)
                return Failure.Conflict($"Participant '{name}' has {topicCount} topics, but the maximum is {tournament.TopicsPerParticipantMax}.");
        }

        return Result.Success();
    }

    /// <summary>
    /// Every user who passed a tournament permission check is a participant of it, so a missing participant is a bug.
    /// </summary>
    private async Task<GetTournamentDto> ToGetDtoForCurrentUserAsync(Tournament tournament)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        TournamentParticipant participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUserId, tournament.Id)
            ?? throw new InvalidOperationException($"User {currentUserId} passed a permission check on tournament {tournament.Id} but isn't a participant of it.");

        return tournament.ToGetDto(participant.Role);
    }

    private static TournamentSettings ResolveSettings(int? playersPerTournament, int? topicsPerParticipantMax, int? topicsPerParticipantMin) =>
        new(
            playersPerTournament ?? DefaultSettings.PlayersPerTournament,
            topicsPerParticipantMax ?? DefaultSettings.TopicsPerParticipantMax,
            topicsPerParticipantMin ?? DefaultSettings.TopicsPerParticipantMin);

    /// <summary>
    /// The tournament form is form-like input, so every problem is collected.
    /// </summary>
    private static List<FieldError> ValidateSettings(TournamentSettings settings)
    {
        List<FieldError> fieldErrors = [];

        if (settings.PlayersPerTournament != DefaultSettings.PlayersPerTournament)
            fieldErrors.Add(new FieldError("playersPerTournament", $"Only {DefaultSettings.PlayersPerTournament} players per tournament are currently supported."));

        if (settings.TopicsPerParticipantMin < 0)
            fieldErrors.Add(new FieldError("topicsPerParticipantMin", "The minimum number of topics can't be negative."));

        if (settings.TopicsPerParticipantMax < settings.TopicsPerParticipantMin)
            fieldErrors.Add(new FieldError("topicsPerParticipantMax", $"The maximum number of topics can't be less than the minimum ({settings.TopicsPerParticipantMin})."));

        return fieldErrors;
    }

    private static bool IsMatchNumberUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: MatchConfiguration.TournamentIdNumberInTournamentIndexName,
        };

    private static Failure TournamentNotFound() =>
        Failure.NotFound("Tournament not found.");

    private static Failure AlreadyStarted() =>
        Failure.Conflict("The tournament has already started. It can only advance to Qualifications from the Preparations stage.");

    private readonly record struct TournamentSettings(int PlayersPerTournament, int TopicsPerParticipantMax, int TopicsPerParticipantMin);
}
