namespace Cbo.Api.Contracts.Topics;

public record UpdateTopicRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required bool IsAuthor { get; set; }
    public required ICollection<UpdateQuestionRequest> Questions { get; set; }
}
