namespace Cbo.API.Domain.Models;

public class Round
{
    public int Id { get; set; }
    public int RoundInSeries { get; set; }
    public int TopicId { get; set; }
    public Topic Topic { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; }

    public ICollection<RoundAnswer> RoundAnswers { get; set; }
} 
