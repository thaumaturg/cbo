namespace Cbo.API.Models.DTO;

public record ChosenTopicRequest
{
    public required Guid TopicId { get; set; }
    public required int PriorityIndex { get; set; }
}
