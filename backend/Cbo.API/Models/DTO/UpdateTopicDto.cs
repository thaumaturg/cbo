namespace Cbo.API.Models.DTO;

public record UpdateTopicDto
{
    public required string Title { get; set; }
    public required bool IsPlayed { get; set; }
}
