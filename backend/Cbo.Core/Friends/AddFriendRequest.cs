using Cbo.Core.Validation;

namespace Cbo.Core.Friends;

public record AddFriendRequest
{
    public required string Username { get; set => field = RequestText.Trim(value); }
}
