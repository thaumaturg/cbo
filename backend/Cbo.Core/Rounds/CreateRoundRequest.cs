namespace Cbo.Core.Rounds;

public record CreateRoundRequest
{
    public required int NumberInMatch { get; set; }
    public required Guid TopicId { get; set; }
    public bool IsOverrideMode { get; set; }
    public List<CreateRoundAnswerRequest> Answers { get; set; } = [];
}
