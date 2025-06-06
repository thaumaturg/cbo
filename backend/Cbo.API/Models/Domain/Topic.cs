namespace Cbo.API.Models.Domain;

public class Topic
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public required int PriorityIndex { get; set; }
    public required bool IsGuest { get; set; }
    public required bool IsPlayed { get; set; }
    public Round? Round { get; set; }
    public ICollection<Question> Questions {  get; set; } = [];
    public ICollection<TournamentTopic> TournamentTopics { get; set; } = [];
    public ICollection<TopicAuthor> TopicAuthors { get; set; } = [];
}
