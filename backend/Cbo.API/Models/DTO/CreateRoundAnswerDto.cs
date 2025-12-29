namespace Cbo.API.Models.DTO;

public record CreateRoundAnswerDto
{
    public required bool IsAnswerAccepted { get; set; }
    public Guid? RoundId { get; set; }
    public required Guid QuestionId { get; set; }
    public required Guid MatchParticipantId { get; set; }
}
