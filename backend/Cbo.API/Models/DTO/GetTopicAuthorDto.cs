namespace Cbo.API.Models.DTO;

public record GetTopicAuthorDto
{
    public required int Id { get; set; }
    public required string Username { get; set; }
    public required bool IsOwner { get; set; }
    public required bool IsAuthor { get; set; }
    public required int TopicId { get; set; }
    public required int ApplicationUserId { get; set; }
}
