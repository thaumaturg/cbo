namespace Cbo.Core.Topics;

public record ImportTopicRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required ICollection<CreateQuestionRequest> Questions { get; set; }
}
