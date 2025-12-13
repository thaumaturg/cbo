namespace Cbo.API.Models.DTO;

public record UpdateTournamentTopicDto
{
    public required int TopicId { get; set; }
    public required int PriorityIndex { get; set; }
}
