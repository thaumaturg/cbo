namespace Cbo.API.Models.Domain;

public class MatchParticipant
{
    public Guid Id { get; set; }
    public int? ScoreSum { get; set; }
    public decimal? PointsSum { get; set; }
    public required Guid TournamentParticipantId { get; set; }
    public required TournamentParticipant TournamentParticipant { get; set; }
    public Guid MatchId { get; set; }
    public required Match Match { get; set; }
    public Guid? PromotedFromId { get; set; }
    public MatchParticipant? PromotedFrom { get; set; }
    public MatchParticipant? PromotedTo { get; set; }
    public ICollection<RoundAnswer> RoundAnswers { get; set; } = [];
}
