namespace Cbo.Api.Contracts.Rounds;

public record CreateRoundAnswerRequest
{
    public bool? IsAnswerAccepted { get; set; }
    public int? OverrideCost { get; set; }
    public required Guid QuestionId { get; set; }
    public required Guid MatchParticipantId { get; set; }
}
