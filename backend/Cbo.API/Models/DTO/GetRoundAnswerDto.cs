namespace Cbo.API.Models.DTO;

public record GetRoundAnswerDto
{
    public required Guid Id { get; set; }
    public bool? IsAnswerAccepted { get; set; }
    public int? OverrideCost { get; set; }
    public required Guid QuestionId { get; set; }
    public required Guid MatchParticipantId { get; set; }
}
