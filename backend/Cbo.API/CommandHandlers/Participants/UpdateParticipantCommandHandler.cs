using Cbo.API.Authorization;
using Cbo.API.Commands.Participants;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Participants;

/// <summary>
/// Changes a participant's role. A new player gets the next seed; a player who leaves the role loses their seed and
/// the remaining players are renumbered.
/// </summary>
public sealed class UpdateParticipantCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : CommandHandler<UpdateParticipantCommand>(dbContext)
{
    private const string ManageParticipantsForbiddenMessage = "You don't have permission to manage the participants of this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    protected override async Task<Result> ExecuteAsync(UpdateParticipantCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ManageParticipants, ManageParticipantsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        TournamentParticipant? participant = await DbContext.TournamentParticipants
            .FirstOrDefaultAsync(tp => tp.Id == command.ParticipantId && tp.TournamentId == command.TournamentId, cancellationToken);
        if (participant is null)
            return ParticipantFailures.NotFound();

        if (participant.Role == TournamentParticipantRole.Creator)
            return Failure.Invalid("The tournament's creator can't be given another role.");

        if (command.Role == TournamentParticipantRole.Creator)
            return ParticipantFailures.InvalidField("role", "A participant can't be promoted to creator.");

        if (access.Value.CurrentStage != TournamentStage.Preparations)
            return ParticipantFailures.Locked();

        bool becomesOrganizer = command.Role == TournamentParticipantRole.Organizer && participant.Role != TournamentParticipantRole.Organizer;
        if (becomesOrganizer && await ParticipantRules.HasMaxOrganizersAsync(DbContext, command.TournamentId, cancellationToken))
            return ParticipantFailures.TooManyOrganizers();

        bool becomesPlayer = command.Role == TournamentParticipantRole.Player && participant.Role != TournamentParticipantRole.Player;
        bool leavesPlayerRole = command.Role != TournamentParticipantRole.Player && participant.Role == TournamentParticipantRole.Player;

        if (becomesPlayer)
            participant.Seed = await ParticipantRules.GetNextPlayerSeedAsync(DbContext, command.TournamentId, cancellationToken);
        else if (command.Role != TournamentParticipantRole.Player)
            participant.Seed = null;

        participant.Role = command.Role;

        if (leavesPlayerRole)
            await ParticipantRules.CompactPlayerSeedsAsync(DbContext, command.TournamentId, participant.Id, cancellationToken);

        return Result.Success();
    }
}
