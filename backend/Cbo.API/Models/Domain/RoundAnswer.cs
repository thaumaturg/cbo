namespace Cbo.API.Models.Domain;

public class RoundAnswer
{
    public required int Id { get; set; }
    public required bool IsAnswerAccepted { get; set; }
    public required DateTime AnsweredAt { get; set; }
    public required int RoundId { get; set; }
    public required Round Round { get; set; }
    public required int QuestionId { get; set; }
    public required Question Question { get; set; }
    public required int MatchParticipantId { get; set; }
    public required MatchParticipant MatchParticipant { get; set; }
}
