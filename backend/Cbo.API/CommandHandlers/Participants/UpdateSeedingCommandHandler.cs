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

public sealed class UpdateSeedingCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : CommandHandler<UpdateSeedingCommand>(dbContext)
{
    private const string ManageParticipantsForbiddenMessage = "You don't have permission to manage the participants of this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    protected override async Task<Result> ExecuteAsync(UpdateSeedingCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ManageParticipants, ManageParticipantsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Dictionary<Guid, TournamentParticipant> playersById = await DbContext.TournamentParticipants
            .Where(tp => tp.TournamentId == command.TournamentId && tp.Role == TournamentParticipantRole.Player)
            .ToDictionaryAsync(tp => tp.Id, cancellationToken);

        if (command.OrderedParticipantIds.Count != playersById.Count || !playersById.Keys.ToHashSet().SetEquals(command.OrderedParticipantIds))
            return Failure.Invalid(
                "Some of the seeding data is invalid.",
                new FieldError("orderedParticipantIds", "The seeding must list each player of the tournament exactly once."));

        if (access.Value.CurrentStage != TournamentStage.Preparations)
            return Failure.Conflict("The seeding can only be changed during the Preparations stage.");

        for (int position = 0; position < command.OrderedParticipantIds.Count; position++)
            playersById[command.OrderedParticipantIds[position]].Seed = position + 1;

        return Result.Success();
    }
}
