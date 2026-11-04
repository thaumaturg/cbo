namespace Cbo.Core.Topics;

public record CreateQuestionRequest
{
    public required int QuestionNumber { get; set; }
    public required int Cost { get; set; }
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Comment { get; set; }
}
