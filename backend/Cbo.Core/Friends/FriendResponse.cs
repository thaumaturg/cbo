namespace Cbo.Core.Friends;

public record FriendResponse
{
    public required Guid AccountId { get; set; }
    public required string Username { get; set; }
    public required DateTime AddedAt { get; set; }
}
