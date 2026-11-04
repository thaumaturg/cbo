namespace Cbo.Core.Domain;

public class Question : IAuditable
{
    public Guid Id { get; set; }
    public required int QuestionNumber { get; set; }
    public required int Cost { get; set; }
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Comment { get; set; }
    public required Guid TopicId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Topic? Topic { get; set; }
    public ICollection<RoundAnswer> RoundAnswers { get; set; } = [];
}
