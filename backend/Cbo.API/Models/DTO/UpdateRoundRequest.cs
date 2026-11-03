namespace Cbo.API.Models.DTO;

public record UpdateRoundRequest
{
    public required int NumberInMatch { get; set; }
    public required Guid TopicId { get; set; }
    public bool IsOverrideMode { get; set; }
    public List<UpdateRoundAnswerRequest> Answers { get; set; } = [];
}
