using Cbo.API.Authorization;
using Cbo.API.Data;
using Cbo.API.Data.Configuration;
using Cbo.API.Mappings;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cbo.API.Services;

public interface ITournamentParticipantService
{
    Task<Result<List<GetTournamentParticipantDto>>> GetParticipantsAsync(Guid tournamentId, TournamentParticipantRole? role);
    Task<Result<GetTournamentParticipantDto>> GetParticipantAsync(Guid tournamentId, Guid participantId);
    Task<Result<GetTournamentParticipantDto>> CreateParticipantAsync(Guid tournamentId, CreateTournamentParticipantDto createParticipantDto);
    Task<Result<GetTournamentParticipantDto>> UpdateParticipantAsync(Guid tournamentId, Guid participantId, UpdateTournamentParticipantDto updateParticipantDto);
    Task<Result<List<GetTournamentParticipantDto>>> UpdateSeedingAsync(Guid tournamentId, UpdateParticipantsSeedingDto seedingDto);
    Task<Result> DeleteParticipantAsync(Guid tournamentId, Guid participantId);
}

public class TournamentParticipantService(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    ITournamentParticipantsRepository participantsRepository,
    UserManager<ApplicationUser> userManager) : ITournamentParticipantService
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string ManageParticipantsForbiddenMessage = "You don't have permission to manage the participants of this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ITournamentParticipantsRepository _participantsRepository = participantsRepository;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<List<GetTournamentParticipantDto>>> GetParticipantsAsync(Guid tournamentId, TournamentParticipantRole? role)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.Read, ReadForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        List<TournamentParticipant> participants = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId, role);

        return participants.Select(p => p.ToGetDto()).ToList();
    }

    public async Task<Result<GetTournamentParticipantDto>> GetParticipantAsync(Guid tournamentId, Guid participantId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.Read, ReadForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        TournamentParticipant? participant = await _participantsRepository.GetByParticipantIdAndTournamentIdAsync(participantId, tournamentId);
        if (participant is null)
            return ParticipantNotFound();

        return participant.ToGetDto();
    }

    public async Task<Result<GetTournamentParticipantDto>> CreateParticipantAsync(Guid tournamentId, CreateTournamentParticipantDto createParticipantDto)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.ManageParticipants, ManageParticipantsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        if (createParticipantDto.Role == TournamentParticipantRole.Creator)
            return InvalidField("role", "A tournament has only one creator, so nobody can be added as a creator.");

        ApplicationUser? user = await _userManager.FindByNameAsync(createParticipantDto.Username);
        if (user is null)
            return InvalidField("username", $"User '{createParticipantDto.Username}' doesn't exist.");

        if (createParticipantDto.Role == TournamentParticipantRole.Organizer && await HasMaxOrganizersAsync(tournamentId))
            return TooManyOrganizers();

        TournamentParticipant? existingParticipant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(user.Id, tournamentId);
        if (existingParticipant is not null)
            return AlreadyAParticipant(createParticipantDto.Username);

        TournamentParticipant participant = createParticipantDto.ToNewParticipant(tournamentId, user.Id);

        if (createParticipantDto.Role == TournamentParticipantRole.Player)
            participant.Seed = await GetNextPlayerSeedAsync(tournamentId);

        try
        {
            participant = await _participantsRepository.CreateAsync(participant);
        }
        catch (DbUpdateException exception) when (IsParticipantUniqueViolation(exception))
        {
            // Lost a race with a simultaneous add of the same user
            return AlreadyAParticipant(createParticipantDto.Username);
        }

        return participant.ToGetDto();
    }

    public async Task<Result<GetTournamentParticipantDto>> UpdateParticipantAsync(Guid tournamentId, Guid participantId, UpdateTournamentParticipantDto updateParticipantDto)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.ManageParticipants, ManageParticipantsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        TournamentParticipant? existingParticipant = await _participantsRepository.GetByParticipantIdAndTournamentIdAsync(participantId, tournamentId);
        if (existingParticipant is null)
            return ParticipantNotFound();

        if (existingParticipant.Role == TournamentParticipantRole.Creator)
            return Failure.Invalid("The tournament's creator can't be given another role.");

        if (updateParticipantDto.Role == TournamentParticipantRole.Creator)
            return InvalidField("role", "A participant can't be promoted to creator.");

        bool becomesOrganizer = updateParticipantDto.Role == TournamentParticipantRole.Organizer && existingParticipant.Role != TournamentParticipantRole.Organizer;
        if (becomesOrganizer && await HasMaxOrganizersAsync(tournamentId))
            return TooManyOrganizers();

        bool becomesPlayer = updateParticipantDto.Role == TournamentParticipantRole.Player && existingParticipant.Role != TournamentParticipantRole.Player;
        bool leavesPlayerRole = updateParticipantDto.Role != TournamentParticipantRole.Player && existingParticipant.Role == TournamentParticipantRole.Player;

        int? seed = existingParticipant.Seed;
        if (becomesPlayer)
            seed = await GetNextPlayerSeedAsync(tournamentId);
        else if (updateParticipantDto.Role != TournamentParticipantRole.Player)
            seed = null;

        var updateParameters = new UpdateTournamentParticipantParameters
        {
            Role = updateParticipantDto.Role,
            Seed = seed
        };

        TournamentParticipant? updatedParticipant = null;
        await _dbContext.Database.ExecuteInTransactionAsync(async () =>
        {
            updatedParticipant = await _participantsRepository.UpdateAsync(participantId, updateParameters);

            if (updatedParticipant is not null && leavesPlayerRole)
                await CompactPlayerSeedsAsync(tournamentId);
        });

        if (updatedParticipant is null)
            return ParticipantNotFound();

        return updatedParticipant.ToGetDto();
    }

    public async Task<Result<List<GetTournamentParticipantDto>>> UpdateSeedingAsync(Guid tournamentId, UpdateParticipantsSeedingDto seedingDto)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.ManageParticipants, ManageParticipantsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        List<TournamentParticipant> players = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId, TournamentParticipantRole.Player);

        HashSet<Guid> playerIds = players.Select(p => p.Id).ToHashSet();
        if (seedingDto.OrderedParticipantIds.Count != playerIds.Count || !playerIds.SetEquals(seedingDto.OrderedParticipantIds))
            return Failure.Invalid(
                "Some of the seeding data is invalid.",
                new FieldError("orderedParticipantIds", "The seeding must list each player of the tournament exactly once."));

        if (access.Value.CurrentStage != TournamentStage.Preparations)
            return Failure.Conflict("The seeding can only be changed during the Preparations stage.");

        List<TournamentParticipant> reseededPlayers = await _participantsRepository.UpdateSeedsAsync(tournamentId, seedingDto.OrderedParticipantIds);

        return reseededPlayers.Select(p => p.ToGetDto()).ToList();
    }

    public async Task<Result> DeleteParticipantAsync(Guid tournamentId, Guid participantId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.ManageParticipants, ManageParticipantsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        TournamentParticipant? existingParticipant = await _participantsRepository.GetByParticipantIdAndTournamentIdAsync(participantId, tournamentId);
        if (existingParticipant is null)
            return ParticipantNotFound();

        if (existingParticipant.Role == TournamentParticipantRole.Creator)
            return Failure.Invalid("The tournament's creator can't be removed.");

        TournamentParticipant? deletedParticipant = null;
        try
        {
            await _dbContext.Database.ExecuteInTransactionAsync(async () =>
            {
                deletedParticipant = await _participantsRepository.DeleteAsync(participantId);

                if (deletedParticipant?.Role == TournamentParticipantRole.Player)
                    await CompactPlayerSeedsAsync(tournamentId);
            });
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // The participant's match history still references them
            return Failure.Conflict("This participant has already played in a match, so they can't be removed.");
        }

        if (deletedParticipant is null)
            return ParticipantNotFound();

        return Result.Success();
    }

    private async Task<bool> HasMaxOrganizersAsync(Guid tournamentId)
    {
        List<TournamentParticipant> organizers = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId, TournamentParticipantRole.Organizer);

        return organizers.Count >= DefaultSettings.OrganizersPerTournamentMax;
    }

    /// <summary>
    /// Returns the seed for a newly added player: one past the highest seed currently taken.
    /// </summary>
    private async Task<int> GetNextPlayerSeedAsync(Guid tournamentId)
    {
        List<TournamentParticipant> players = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId, TournamentParticipantRole.Player);

        return players.Count == 0 ? 1 : players.Max(p => p.Seed ?? 0) + 1;
    }

    /// <summary>
    /// Renumbers player seeds to a contiguous 1..N sequence, preserving the current order.
    /// Used after a player leaves the seeding (removed or demoted to another role).
    /// </summary>
    private async Task CompactPlayerSeedsAsync(Guid tournamentId)
    {
        List<TournamentParticipant> players = await _participantsRepository.GetAllByTournamentIdAsync(tournamentId, TournamentParticipantRole.Player);

        List<Guid> orderedIds = players
            .OrderBy(p => p.Seed ?? int.MaxValue)
            .ThenBy(p => p.Id)
            .Select(p => p.Id)
            .ToList();

        await _participantsRepository.UpdateSeedsAsync(tournamentId, orderedIds);
    }

    private static bool IsParticipantUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: TournamentParticipantConfiguration.TournamentIdApplicationUserIdIndexName,
        };

    private static Failure InvalidField(string field, string message) =>
        Failure.Invalid("Some of the participant data is invalid.", new FieldError(field, message));

    private static Failure ParticipantNotFound() =>
        Failure.NotFound("Participant not found in this tournament.");

    private static Failure TooManyOrganizers() =>
        Failure.Conflict($"A tournament can have at most {DefaultSettings.OrganizersPerTournamentMax} organizers.");

    private static Failure AlreadyAParticipant(string username) =>
        Failure.Conflict($"User '{username}' is already a participant in this tournament.");
}
