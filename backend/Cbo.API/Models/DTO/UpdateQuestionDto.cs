namespace Cbo.API.Models.DTO;

public record UpdateQuestionDto
{
    public required Guid Id { get; set; }
    public required int QuestionNumber { get; set; }
    public required int CostPositive { get; set; }
    public required int CostNegative { get; set; }
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Comment { get; set; }
}
