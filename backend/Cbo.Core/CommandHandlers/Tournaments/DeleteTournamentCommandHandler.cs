using Cbo.Core.Authorization;
using Cbo.Core.Commands.Tournaments;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Models.Domain;
using Cbo.Core.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.CommandHandlers.Tournaments;

public sealed class DeleteTournamentCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : CommandHandler<DeleteTournamentCommand>(dbContext)
{
    private const string DeleteForbiddenMessage = "You don't have permission to delete this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    protected override async Task<Result> ExecuteAsync(DeleteTournamentCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.Delete, DeleteForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        // Round answers and match participants block the cascade from the tournament, so they go first
        await DbContext.RoundAnswers
            .Where(ra => ra.Round!.Match!.TournamentId == command.TournamentId)
            .ExecuteDeleteAsync(cancellationToken);

        await DbContext.MatchParticipants
            .Where(mp => mp.Match!.TournamentId == command.TournamentId)
            .ExecuteDeleteAsync(cancellationToken);

        DbContext.Tournaments.Remove(access.Value);

        return Result.Success();
    }
}
