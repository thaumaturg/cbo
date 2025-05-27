namespace Cbo.API.Domain.Models;

public class RoundAnswer
{
    public int Id { get; set; }
    public bool IsAnswerAccepted { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public int RoundId { get; set; }
    public Round Round { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; }
    public int MatchParticipantId { get; set; }
    public MatchParticipant MatchParticipant { get; set; }
} 
