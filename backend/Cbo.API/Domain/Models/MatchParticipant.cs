namespace Cbo.API.Domain.Models;

public class MatchParticipant
{
    public int Id { get; set; }
    public int TournamentParticipantId { get; set; }
    public TournamentParticipant TournamentParticipant { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; }
    public int SourceMatchId { get; set; }
    public Match SourceMatch { get; set; }

    public ICollection<RoundAnswer> RoundAnswers { get; set; }
} 
