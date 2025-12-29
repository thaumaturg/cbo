namespace Cbo.API.Models.DTO;

public record GetRoundAnswerDto
{
    public required Guid Id { get; set; }
    public required bool IsAnswerAccepted { get; set; }
    public required Guid QuestionId { get; set; }
    public required Guid MatchParticipantId { get; set; }
}
