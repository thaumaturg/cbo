namespace Cbo.API.Models.Domain;

public class TopicAuthor
{
    public int Id { get; set; }
    public bool IsOwner { get; set; }
    public bool IsAuthor { get; set; }
    public int ApplicationUserId { get; set; }
    public required ApplicationUser ApplicationUser { get; set; }
    public int TopicId { get; set; }
    public required Topic Topic { get; set; }
}
