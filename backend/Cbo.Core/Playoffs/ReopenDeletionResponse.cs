using Cbo.Core.Domain;

namespace Cbo.Core.Playoffs;

/// <summary>A match that reopening another match would delete, for the Reopen confirm dialog.</summary>
public record ReopenDeletionResponse
{
    public required Guid MatchId { get; set; }
    public required MatchType Type { get; set; }

    /// <summary>Where a playoff match or the Final sits in the bracket. Null for a shootout.</summary>
    public BracketPosition? BracketPosition { get; set; }

    /// <summary>Where the match a shootout settles sits in the bracket. Null for other kinds of match.</summary>
    public BracketPosition? SettledBracketPosition { get; set; }

    /// <summary>A shootout's sequence number among the shootouts of the match it settles, from 1. Null for other kinds of match.</summary>
    public int? ShootoutNumber { get; set; }

    public required int RoundsCount { get; set; }
}
