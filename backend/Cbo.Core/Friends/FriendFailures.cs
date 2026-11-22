using Cbo.Results;

namespace Cbo.Core.Friends;

/// <summary>
/// Failures shared by the friend command and query handlers.
/// </summary>
internal static class FriendFailures
{
    private const string UsernameField = "username";
    private const string NotAddedMessage = "The friend couldn't be added.";

    public static Failure InvalidUsername(string message) =>
        Failure.Invalid(NotAddedMessage, new FieldError(UsernameField, message));

    // A field error as well, so the form shows it under the username box
    public static Failure AlreadyAFriend(string username) =>
        Failure.Conflict(NotAddedMessage, new FieldError(UsernameField, $"'{username}' is already on your friend list."));

    public static Failure NotFound() =>
        Failure.NotFound("This account isn't on your friend list.");
}
