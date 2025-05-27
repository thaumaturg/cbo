namespace Cbo.API.Domain.Models;

public class TournamentTopic
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; }
    public int TopicId { get; set; }
    public Topic Topic { get; set; }
} 
