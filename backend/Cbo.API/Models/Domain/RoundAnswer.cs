namespace Cbo.API.Models.Domain;

public class RoundAnswer
{
    public Guid Id { get; set; }
    public bool? IsAnswerAccepted { get; set; }
    public int? OverrideCost { get; set; }
    public required Guid RoundId { get; set; }
    public required Round Round { get; set; }
    public required Guid QuestionId { get; set; }
    public required Question Question { get; set; }
    public required Guid MatchParticipantId { get; set; }
    public required MatchParticipant MatchParticipant { get; set; }
}
