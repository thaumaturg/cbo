namespace Cbo.Core.Domain;

public class MatchParticipant : IAuditable
{
    public Guid Id { get; set; }
    public int? ScoreSum { get; set; }
    public int? PositiveScoreSum { get; set; }
    public decimal? PointsSum { get; set; }

    /// <summary>What the player got from a playoff match or the Final once it is Decided. Null until then.</summary>
    public Outcome? Outcome { get; set; }

    public required Guid TournamentParticipantId { get; set; }
    public required Guid MatchId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public TournamentParticipant? TournamentParticipant { get; set; }
    public Match? Match { get; set; }
    public ICollection<RoundAnswer> RoundAnswers { get; set; } = [];
}
