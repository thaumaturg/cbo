namespace Cbo.API.Models.Domain;

public class TopicAuthor
{
    public int Id { get; set; }
    public bool IsOwner { get; set; }
    public bool IsAuthor { get; set; }
    public required int ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
    public int TopicId { get; set; }
    public Topic? Topic { get; set; }
}
