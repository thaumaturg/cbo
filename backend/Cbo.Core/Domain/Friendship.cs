namespace Cbo.Core.Domain;

/// <summary>
/// One entry on an account's friend list. One-sided: <see cref="FriendId"/> isn't asked or told.
/// </summary>
public class Friendship : IAuditable
{
    public Guid Id { get; set; }
    public required Guid ApplicationUserId { get; set; }
    public required Guid FriendId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ApplicationUser? ApplicationUser { get; set; }
    public ApplicationUser? Friend { get; set; }
}
