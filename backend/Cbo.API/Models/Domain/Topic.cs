namespace Cbo.API.Models.Domain;

public class Topic
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public ICollection<Round> Rounds { get; set; } = [];
    public ICollection<Question> Questions { get; set; } = [];
    public ICollection<TournamentTopic> TournamentTopics { get; set; } = [];
    public ICollection<TopicAuthor> TopicAuthors { get; set; } = [];
}
