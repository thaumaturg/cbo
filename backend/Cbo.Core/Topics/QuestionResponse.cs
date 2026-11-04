namespace Cbo.Core.Topics;

public record QuestionResponse
{
    public required Guid Id { get; set; }
    public required int QuestionNumber { get; set; }
    public required int Cost { get; set; }
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Comment { get; set; }
    public required Guid TopicId { get; set; }
}
