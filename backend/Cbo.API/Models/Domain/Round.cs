namespace Cbo.API.Models.Domain;

public class Round
{
    public required int Id { get; set; }
    public required int NumberInMatch { get; set; }
    public required int TopicId { get; set; }
    public required Topic Topic { get; set; }
    public required int MatchId { get; set; }
    public required Match Match { get; set; }
    public ICollection<RoundAnswer> RoundAnswers { get; set; } = [];
}
