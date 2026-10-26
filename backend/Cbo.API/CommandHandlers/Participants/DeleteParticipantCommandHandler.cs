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
/// Removes a participant. If they were a player, the remaining players are renumbered.
/// </summary>
public sealed class DeleteParticipantCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : CommandHandler<DeleteParticipantCommand>(dbContext)
{
    private const string ManageParticipantsForbiddenMessage = "You don't have permission to manage the participants of this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    protected override async Task<Result> ExecuteAsync(DeleteParticipantCommand command, CancellationToken cancellationToken)
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
            return Failure.Invalid("The tournament's creator can't be removed.");

        if (access.Value.CurrentStage != TournamentStage.Preparations)
            return ParticipantFailures.Locked();

        DbContext.TournamentParticipants.Remove(participant);

        if (participant.Role == TournamentParticipantRole.Player)
            await ParticipantRules.CompactPlayerSeedsAsync(DbContext, command.TournamentId, participant.Id, cancellationToken);

        return Result.Success();
    }

    protected override Failure? MapSaveFailure(DeleteParticipantCommand command, DbUpdateException exception)
    {
        // The participant's match history still references them (the tournament started meanwhile)
        if (PostgresConstraint.IsForeignKeyViolation(exception))
            return Failure.Conflict("This participant has already played in a match, so they can't be removed.");

        return null;
    }
}
