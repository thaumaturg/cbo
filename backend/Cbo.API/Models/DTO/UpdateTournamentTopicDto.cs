namespace Cbo.API.Models.DTO;

public record UpdateTournamentTopicDto
{
    public required Guid TopicId { get; set; }
    public required int PriorityIndex { get; set; }
}
