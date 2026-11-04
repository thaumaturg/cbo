namespace Cbo.Api.Contracts.Topics;

public record TopicAuthorResponse
{
    public required Guid Id { get; set; }
    public required string Username { get; set; }
    public required bool IsOwner { get; set; }
    public required bool IsAuthor { get; set; }
    public required Guid TopicId { get; set; }
    public required Guid ApplicationUserId { get; set; }
}
