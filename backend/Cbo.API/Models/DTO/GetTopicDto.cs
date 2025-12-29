namespace Cbo.API.Models.DTO;

public record GetTopicDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required bool IsPlayed { get; set; }
    public bool IsAuthor { get; set; }
    public ICollection<GetQuestionDto> Questions { get; set; } = [];
}
