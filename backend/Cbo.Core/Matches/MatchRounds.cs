using Cbo.Core.Domain;

namespace Cbo.Core.Matches;

/// <summary>How many rounds a match plays, which depends on its kind.</summary>
public static class MatchRounds
{
    /// <summary>Rounds a match of this kind plays. A match with all of them played is finished.</summary>
    public static int RoundsToPlay(this MatchType type) =>
        Limits.Matches.RoundsPerMatch.TryGetValue(type, out int rounds)
            ? rounds
            : throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown match type.");

    /// <summary>Whether a match of this kind with this many rounds played is finished.</summary>
    public static bool IsFinished(this MatchType type, int roundsPlayed) => roundsPlayed == type.RoundsToPlay();
}
