namespace Cbo.Core.Participants;

/// <summary>A player's qualification results, as far as their <see cref="QualificationPlaces"/> go.</summary>
public sealed record QualificationResult(decimal Points, int Score, int PositiveScore, int Seed);

/// <summary>Ranks players by qualification place: points, then score, then positive score, then seed.</summary>
public static class QualificationPlaces
{
    /// <summary>The players from the 1st qualification place down. No two players share a place.</summary>
    public static IEnumerable<T> InPlaceOrder<T>(IEnumerable<T> players, Func<T, QualificationResult> resultOf) =>
        players
            .Select(player => (Player: player, Result: resultOf(player)))
            .OrderByDescending(entry => entry.Result.Points)
            .ThenByDescending(entry => entry.Result.Score)
            .ThenByDescending(entry => entry.Result.PositiveScore)
            .ThenBy(entry => entry.Result.Seed)
            .Select(entry => entry.Player);
}
