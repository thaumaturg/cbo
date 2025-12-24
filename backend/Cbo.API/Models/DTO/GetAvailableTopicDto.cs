namespace Cbo.API.Models.DTO;

public record GetAvailableTopicDto
{
    public required int PriorityIndex { get; set; }
    public required int TopicId { get; set; }
    public required string TopicTitle { get; set; }
    public required string OwnerUsername { get; set; }
}
