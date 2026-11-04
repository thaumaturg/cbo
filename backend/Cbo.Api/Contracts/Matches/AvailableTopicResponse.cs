namespace Cbo.Api.Contracts.Matches;

public record AvailableTopicResponse
{
    public required int PriorityIndex { get; set; }
    public required Guid TopicId { get; set; }
    public required string TopicTitle { get; set; }
    public required string OwnerUsername { get; set; }
}
