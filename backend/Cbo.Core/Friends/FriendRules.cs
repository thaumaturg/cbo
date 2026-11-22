using Cbo.Results;

namespace Cbo.Core.Friends;

/// <summary>
/// Checks whether an account can be added to a friend list. Pure, so the handler looks the account up first.
/// </summary>
public static class FriendRules
{
    /// <summary>
    /// Returns the first reason <paramref name="username"/> can't go on <paramref name="currentUserId"/>'s friend list, or
    /// null when it can. <paramref name="friendId"/> is the account the username resolved to, null when none has it.
    /// </summary>
    public static Failure? CheckNewFriend(string? username, Guid? friendId, Guid currentUserId, bool isAlreadyAFriend)
    {
        if (string.IsNullOrWhiteSpace(username))
            return FriendFailures.InvalidUsername("Username is required.");

        if (friendId is null)
            return FriendFailures.InvalidUsername($"There's no account with the username '{username}'.");

        if (friendId == currentUserId)
            return FriendFailures.InvalidUsername("You can't add yourself as a friend.");

        if (isAlreadyAFriend)
            return FriendFailures.AlreadyAFriend(username);

        return null;
    }
}
