namespace Cbo.API.Models.DTO;

public class CreateQuestionDto
{
    public int? QuestionNumber { get; set; }
    public int? CostPositive { get; set; }
    public int? CostNegative { get; set; }
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Comment { get; set; }
}
