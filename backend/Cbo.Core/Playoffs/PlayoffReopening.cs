using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Playoffs;

/// <summary>
/// What reopening a confirmed playoff match, shootout or Final changes: it deletes the shootouts after it, every
/// existing match after the match it settles in the bracket and their shootouts, and clears that match's Outcomes.
/// See docs/adr/0012-playoff-results-are-confirmed-and-corrected-by-reopening.md.
/// </summary>
/// <param name="SettledMatchId">The match whose Outcomes Reopen clears: the match itself, or the one a shootout settles.</param>
/// <param name="DeletedMatchIds">The matches Reopen deletes, with their rounds.</param>
internal sealed record PlayoffReopening(Guid SettledMatchId, IReadOnlySet<Guid> DeletedMatchIds)
{
    /// <summary>Why the match can't be reopened, or null when it can.</summary>
    public static Failure? Check(Tournament tournament, Match match)
    {
        if (tournament.CurrentStage != TournamentStage.Playoff)
            return PlayoffFailures.NotInPlayoff("reopened");

        if (match.Type == MatchType.Qualification)
            return PlayoffFailures.QualificationMatch("reopened");

        if (match.ConfirmedAt is null)
            return Failure.Conflict("Only a confirmed match can be reopened.");

        return null;
    }

    /// <summary>What reopening the match changes, from the bracket template and the matches that exist.</summary>
    /// <param name="match">A playoff match, shootout or Final of the tournament.</param>
    public static async Task<PlayoffReopening> PlanAsync(
        CboDbContext dbContext, Tournament tournament, Match match, CancellationToken cancellationToken)
    {
        PlayoffBracket bracket = PlayoffBracket.For(tournament.Format)
            ?? throw new InvalidOperationException($"Format {tournament.Format} has no playoff bracket.");

        Guid settledMatchId = match.SettledMatchId ?? match.Id;

        var playoffMatches = await dbContext.Matches
            .Where(m => m.TournamentId == tournament.Id && m.Type != MatchType.Qualification)
            .Select(m => new { m.Id, m.BracketPosition, m.SettledMatchId, m.ShootoutNumber })
            .ToListAsync(cancellationToken);

        Dictionary<BracketPosition, Guid> byPosition = playoffMatches
            .Where(m => m.BracketPosition is not null)
            .ToDictionary(m => m.BracketPosition!.Value, m => m.Id);
        Dictionary<int, Guid> shootoutsByNumber = playoffMatches
            .Where(m => m.SettledMatchId == settledMatchId)
            .ToDictionary(m => m.ShootoutNumber!.Value, m => m.Id);

        BracketPosition position = playoffMatches.Single(m => m.Id == settledMatchId).BracketPosition!.Value;
        ReopenDeletion deletion = bracket.DeletedByReopening(
            position, match.ShootoutNumber, byPosition.Keys, shootoutsByNumber.Keys);

        HashSet<Guid> deletedLaterMatches = [.. deletion.Positions.Select(p => byPosition[p])];
        HashSet<Guid> deleted =
        [
            .. deletion.Shootouts.Select(number => shootoutsByNumber[number]),
            .. deletedLaterMatches,
            .. playoffMatches.Where(m => m.SettledMatchId is Guid settled && deletedLaterMatches.Contains(settled)).Select(m => m.Id),
        ];

        return new PlayoffReopening(settledMatchId, deleted);
    }
}
