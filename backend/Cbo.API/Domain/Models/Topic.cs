namespace Cbo.API.Domain.Models;

public class Topic
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int PriorityIndex { get; set; }
    public bool IsGuest { get; set; }
    public bool IsPlayed { get; set; }

    public Round? Round { get; set; }
    public ICollection<Question> Questions { get; set; }
    public ICollection<TournamentTopic> TournamentTopics { get; set; }
    public ICollection<TopicAuthor> TopicAuthors { get; set; }
} 
