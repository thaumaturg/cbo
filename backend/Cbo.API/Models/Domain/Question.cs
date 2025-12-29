namespace Cbo.API.Models.Domain;

public class Question
{
    public Guid Id { get; set; }
    public required int QuestionNumber { get; set; }
    public required int CostPositive { get; set; }
    public required int CostNegative { get; set; }
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Comment { get; set; }
    public required Guid TopicId { get; set; }
    public required Topic Topic { get; set; }
    public ICollection<RoundAnswer> RoundAnswers { get; set; } = [];
}
