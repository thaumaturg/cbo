using Cbo.Core.Domain;

namespace Cbo.Core.Matches;

/// <summary>Why a match's rounds can no longer be added, changed or deleted.</summary>
public enum MatchLockReason
{
    /// <summary>The Creator confirmed the playoff match, shootout or Final.</summary>
    Confirmed,

    /// <summary>A qualification match, and the Playoff has started.</summary>
    PlayoffStarted,
}

/// <summary>Whether a match is Locked, see the glossary.</summary>
public static class MatchLock
{
    /// <summary>Why the match is locked, or null when its rounds can still be edited.</summary>
    public static MatchLockReason? ReasonFor(MatchType type, DateTime? confirmedAt, TournamentStage stage)
    {
        if (confirmedAt is not null)
            return MatchLockReason.Confirmed;

        if (type == MatchType.Qualification && stage is TournamentStage.Playoff or TournamentStage.Ended)
            return MatchLockReason.PlayoffStarted;

        return null;
    }

    public static bool IsLocked(MatchType type, DateTime? confirmedAt, TournamentStage stage) =>
        ReasonFor(type, confirmedAt, stage) is not null;
}
