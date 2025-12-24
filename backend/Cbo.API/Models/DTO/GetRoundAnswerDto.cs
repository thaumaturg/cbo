namespace Cbo.API.Models.DTO;

public record GetRoundAnswerDto
{
    public required int Id { get; set; }
    public required bool IsAnswerAccepted { get; set; }
    public required int QuestionId { get; set; }
    public required int MatchParticipantId { get; set; }
}
