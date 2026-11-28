using Cbo.Core.Domain;

namespace Cbo.Core.Matches;

public record MatchResponse
{
    public required Guid Id { get; set; }
    public required int NumberInTournament { get; set; }
    public required int NumberInStage { get; set; }
    public required TournamentStage CreatedOnStage { get; set; }
    public required MatchType Type { get; set; }
    public required Guid TournamentId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public required bool IsLocked { get; set; }

    /// <summary>Where a playoff match or the Final sits in the bracket. Null for other kinds of match.</summary>
    public BracketPosition? BracketPosition { get; set; }

    /// <summary>The playoff match or Final a shootout settles. Null for other kinds of match.</summary>
    public Guid? SettledMatchId { get; set; }

    /// <summary>A shootout's sequence number among the shootouts of the match it settles, from 1. Null for other kinds of match.</summary>
    public int? ShootoutNumber { get; set; }

    public required List<MatchParticipantResponse> MatchParticipants { get; set; }
    public required int RoundsCount { get; set; }
}
