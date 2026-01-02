namespace Cbo.API.Models.DTO;

public record CreateTopicDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required bool IsAuthor { get; set; }
    public required ICollection<CreateQuestionDto> Questions { get; set; }
}
