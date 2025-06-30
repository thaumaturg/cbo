namespace Cbo.API.Models.DTO;

public class UpdateRoundAnswerDto
{
    public required bool IsAnswerAccepted { get; set; }
    public required int RoundId { get; set; }
    public required int QuestionId { get; set; }
    public required int MatchParticipantId { get; set; }
}
