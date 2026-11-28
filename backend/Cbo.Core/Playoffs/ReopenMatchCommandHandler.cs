using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Tournaments;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Playoffs;

/// <summary>
/// Reopens a confirmed playoff match, shootout or Final: deletes what was created from its result and everything
/// created after that, with their rounds, clears the Outcomes of the match it settles, then unlocks it. Confirming it
/// again rebuilds them. See docs/adr/0012-playoff-results-are-confirmed-and-corrected-by-reopening.md.
/// </summary>
public sealed class ReopenMatchCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : CommandHandler<ReopenMatchCommand>(dbContext)
{
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    protected override async Task<Result> ExecuteAsync(ReopenMatchCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LockTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ReopenMatches, PlayoffFailures.ReopenForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;

        Match? match = await DbContext.Matches
            .FirstOrDefaultAsync(m => m.Id == command.MatchId && m.TournamentId == command.TournamentId, cancellationToken);
        if (match is null)
            return MatchFailures.MatchNotFound();

        Failure? failure = PlayoffReopening.Check(tournament, match);
        if (failure is not null)
            return failure;

        PlayoffReopening reopening = await PlayoffReopening.PlanAsync(DbContext, tournament, match, cancellationToken);

        // Removing a match marks its tracked players deleted at once, which throws while their answers are still
        // there, so the rounds go first and take their answers with them
        List<Match> deleted = await DbContext.Matches
            .Include(m => m.Rounds)
            .ThenInclude(r => r.RoundAnswers)
            .Include(m => m.MatchParticipants)
            .Where(m => reopening.DeletedMatchIds.Contains(m.Id))
            .ToListAsync(cancellationToken);
        DbContext.Rounds.RemoveRange(deleted.SelectMany(m => m.Rounds));
        DbContext.Matches.RemoveRange(deleted);

        List<MatchParticipant> settledPlayers = await DbContext.MatchParticipants
            .Where(mp => mp.MatchId == reopening.SettledMatchId)
            .ToListAsync(cancellationToken);
        foreach (MatchParticipant player in settledPlayers)
            player.Outcome = null;

        match.ConfirmedAt = null;

        return Result.Success();
    }
}
