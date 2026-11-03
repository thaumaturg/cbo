namespace Cbo.API.Contracts.Topics;

public record CreateQuestionRequest
{
    public required int QuestionNumber { get; set; }
    public required int CostPositive { get; set; }
    public required int CostNegative { get; set; }
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Comment { get; set; }
}
