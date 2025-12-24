namespace Cbo.API.Models.DTO;

public record CreateRoundAnswerDto
{
    public required bool IsAnswerAccepted { get; set; }
    public int? RoundId { get; set; }
    public required int QuestionId { get; set; }
    public required int MatchParticipantId { get; set; }
}
