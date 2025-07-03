namespace Cbo.API.Models.Domain;

public class MatchParticipant
{
    public required int Id { get; set; }
    public required int TournamentParticipantId { get; set; }
    public required TournamentParticipant TournamentParticipant { get; set; }
    public required int MatchId { get; set; }
    public required Match Match { get; set; }
    public int? PromotedFromId { get; set; }
    public MatchParticipant? PromotedFrom { get; set; }
    public MatchParticipant? PromotedTo { get; set; }
    public ICollection<RoundAnswer> RoundAnswers { get; set; } = [];
}
