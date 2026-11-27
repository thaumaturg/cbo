using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Data.Configuration;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Tournaments;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Playoffs;

/// <summary>
/// Confirms a finished playoff match, shootout or Final: locks it, then goes on from its result to the next shootout
/// or the next matches. See docs/adr/0012-playoff-results-are-confirmed-and-corrected-by-reopening.md.
/// </summary>
public sealed class ConfirmMatchCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    IPlayoffProgressionService playoffProgressionService) : CommandHandler<ConfirmMatchCommand>(dbContext)
{
    private const string ForbiddenMessage = "You don't have permission to confirm matches in this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly IPlayoffProgressionService _playoffProgressionService = playoffProgressionService;

    protected override async Task<Result> ExecuteAsync(ConfirmMatchCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LockTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ConfirmMatches, ForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;

        Match? match = await DbContext.Matches
            .Include(m => m.MatchParticipants)
            .FirstOrDefaultAsync(m => m.Id == command.MatchId && m.TournamentId == command.TournamentId, cancellationToken);
        if (match is null)
            return Failure.NotFound("Match not found in this tournament.");

        if (tournament.CurrentStage != TournamentStage.Playoff)
            return Failure.Conflict("Matches can only be confirmed while the tournament is in the Playoff stage.");

        if (match.Type == MatchType.Qualification)
            return Failure.Conflict("Only playoff matches, shootouts and the Final are confirmed.");

        if (match.ConfirmedAt is not null)
            return Failure.Conflict("This match is already confirmed.");

        int roundsPlayed = await DbContext.Rounds.CountAsync(r => r.MatchId == match.Id, cancellationToken);
        if (!match.Type.IsFinished(roundsPlayed))
        {
            return Failure.Conflict(
                $"Only a finished match can be confirmed. This one has {roundsPlayed} of its {match.Type.RoundsToPlay()} rounds.");
        }

        match.ConfirmedAt = DateTime.UtcNow;

        await _playoffProgressionService.GoOnFromAsync(tournament, match, cancellationToken);

        return Result.Success();
    }

    protected override Failure? MapSaveFailure(ConfirmMatchCommand command, DbUpdateException exception)
    {
        // A match it would create already exists: another confirmation got there first
        if (PostgresConstraint.IsUniqueViolation(exception, MatchConfiguration.TournamentIdNumberInTournamentIndexName)
            || PostgresConstraint.IsUniqueViolation(exception, MatchConfiguration.SettledMatchIdShootoutNumberIndexName))
        {
            return Failure.Conflict("Another confirmation changed the Playoff at the same time. Reload the tournament and try again.");
        }

        return null;
    }
}
