namespace Cbo.API.Domain.Models;

public class TopicAuthor
{
    public int Id { get; set; }
    public bool IsOwner { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int TopicId { get; set; }
    public Topic Topic { get; set; }
} 
