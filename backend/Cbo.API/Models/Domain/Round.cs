namespace Cbo.API.Models.Domain;

public class Round
{
    public Guid Id { get; set; }
    public required int NumberInMatch { get; set; }
    public bool IsOverrideMode { get; set; }
    public required Guid TopicId { get; set; }
    public required Guid MatchId { get; set; }

    // Navigation properties
    public Topic? Topic { get; set; }
    public Match? Match { get; set; }
    public ICollection<RoundAnswer> RoundAnswers { get; set; } = [];
}
