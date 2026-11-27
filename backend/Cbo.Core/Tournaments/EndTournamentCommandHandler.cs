using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Tournaments;

/// <summary>Moves a tournament from the Playoff to Ended, once its Final is Decided.</summary>
public sealed class EndTournamentCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : CommandHandler<EndTournamentCommand>(dbContext)
{
    private const string ForbiddenMessage = "You don't have permission to end this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    protected override async Task<Result> ExecuteAsync(EndTournamentCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LockTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ChangeStage, ForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;

        if (tournament.CurrentStage != TournamentStage.Playoff)
            return Failure.Conflict("Only a tournament in the Playoff stage can be ended.");

        bool finalDecided = await DbContext.Matches
            .AnyAsync(
                m => m.TournamentId == tournament.Id
                    && m.BracketPosition == BracketPosition.Final
                    && m.MatchParticipants.All(mp => mp.Outcome != null),
                cancellationToken);
        if (!finalDecided)
            return Failure.Conflict("The tournament can't end before its Final is decided.");

        tournament.CurrentStage = TournamentStage.Ended;
        tournament.EndedAt = DateTime.UtcNow;

        return Result.Success();
    }
}
