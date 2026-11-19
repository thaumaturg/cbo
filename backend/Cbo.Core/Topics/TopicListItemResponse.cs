namespace Cbo.Core.Topics;

/// <summary>A row of the topics list: the counts instead of the questions and authors themselves.</summary>
public record TopicListItemResponse
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required int QuestionCount { get; set; }
    public required int AuthorCount { get; set; }
    public required bool IsAuthor { get; set; }
    public required bool IsPlayed { get; set; }
    public required DateTime CreatedAt { get; set; }
}
