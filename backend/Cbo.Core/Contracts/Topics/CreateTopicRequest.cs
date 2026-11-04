namespace Cbo.Core.Contracts.Topics;

public record CreateTopicRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required bool IsAuthor { get; set; }
    public required ICollection<CreateQuestionRequest> Questions { get; set; }
}
