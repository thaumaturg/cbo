namespace Cbo.API.Models.Domain;

public class TopicAuthor
{
    public Guid Id { get; set; }
    public bool IsOwner { get; set; }
    public bool IsAuthor { get; set; }
    public required Guid ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
    public Guid TopicId { get; set; }
    public Topic? Topic { get; set; }
}
