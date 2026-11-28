using Cbo.Core.Domain;

namespace Cbo.Core.Matches;

/// <summary>A player's tournament totals, which count their qualification matches only.</summary>
public readonly record struct QualificationTotals(decimal Points, int Score, int PositiveScore)
{
    /// <summary>Adds up the player's match results. Every one needs its <see cref="MatchParticipant.Match"/>.</summary>
    public static QualificationTotals Of(IEnumerable<MatchParticipant> matchParticipants)
    {
        List<MatchParticipant> qualification = matchParticipants
            .Where(mp => MatchOf(mp).Type == MatchType.Qualification)
            .ToList();

        return new QualificationTotals(
            Points: qualification.Sum(mp => mp.PointsSum ?? 0m),
            Score: qualification.Sum(mp => mp.ScoreSum ?? 0),
            PositiveScore: qualification.Sum(mp => mp.PositiveScoreSum ?? 0));
    }

    private static Match MatchOf(MatchParticipant matchParticipant) =>
        matchParticipant.Match
            ?? throw new InvalidOperationException($"The match of match participant {matchParticipant.Id} isn't loaded.");
}
