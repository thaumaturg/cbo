namespace Cbo.Api.Contracts.Topics;

public record TopicResponse
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required bool IsPlayed { get; set; }
    public bool IsAuthor { get; set; }
    public ICollection<QuestionResponse> Questions { get; set; } = [];
    public ICollection<TopicAuthorResponse> Authors { get; set; } = [];
}
