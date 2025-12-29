namespace Cbo.API.Models.Domain;

public class Round
{
    public Guid Id { get; set; }
    public required int NumberInMatch { get; set; }
    public required Guid TopicId { get; set; }
    public required Topic Topic { get; set; }
    public required Guid MatchId { get; set; }
    public required Match Match { get; set; }
    public ICollection<RoundAnswer> RoundAnswers { get; set; } = [];
}
