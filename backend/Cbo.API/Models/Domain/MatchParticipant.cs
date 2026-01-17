namespace Cbo.API.Models.Domain;

public class MatchParticipant
{
    public Guid Id { get; set; }
    public int? ScoreSum { get; set; }
    public decimal? PointsSum { get; set; }
    public required Guid TournamentParticipantId { get; set; }
    public required Guid MatchId { get; set; }
    public Guid? PromotedFromId { get; set; }

    // Navigation properties
    public TournamentParticipant? TournamentParticipant { get; set; }
    public Match? Match { get; set; }
    public MatchParticipant? PromotedFrom { get; set; }
    public MatchParticipant? PromotedTo { get; set; }
    public ICollection<RoundAnswer> RoundAnswers { get; set; } = [];
}
