namespace Cbo.API.Domain.Models;

public class Question
{
    public int Id { get; set; }
    public int QuestionNumber { get; set; }
    public int CostPositive { get; set; }
    public int CostNegative { get; set; }
    public string Text { get; set; }
    public string Answer { get; set; }
    public string Comment { get; set; }
    public int TopicId { get; set; }
    public Topic Topic { get; set; }

    public ICollection<RoundAnswer> RoundAnswers { get; set; }
} 
