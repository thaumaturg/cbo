using System;
using Cbo.Core.Friends;
using Cbo.Results;

namespace Cbo.Tests.Friends;

public class FriendRulesTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BobId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public void Another_account_that_is_not_a_friend_yet_can_be_added()
    {
        Assert.Null(FriendRules.CheckNewFriend("bob", BobId, CurrentUserId, isAlreadyAFriend: false));
    }

    [Fact]
    public void A_username_without_an_account_is_an_error_naming_it()
    {
        Failure? failure = FriendRules.CheckNewFriend("nobody", null, CurrentUserId, isAlreadyAFriend: false);

        AssertUsernameError(failure, FailureKind.Invalid, "There's no account with the username 'nobody'.");
    }

    [Fact]
    public void Your_own_account_can_not_be_added()
    {
        Failure? failure = FriendRules.CheckNewFriend("me", CurrentUserId, CurrentUserId, isAlreadyAFriend: false);

        AssertUsernameError(failure, FailureKind.Invalid, "You can't add yourself as a friend.");
    }

    [Fact]
    public void Someone_already_on_the_list_is_a_conflict()
    {
        Failure? failure = FriendRules.CheckNewFriend("bob", BobId, CurrentUserId, isAlreadyAFriend: true);

        AssertUsernameError(failure, FailureKind.Conflict, "'bob' is already on your friend list.");
    }

    private static void AssertUsernameError(Failure? failure, FailureKind kind, string message)
    {
        Assert.NotNull(failure);
        Assert.Equal(kind, failure.Kind);
        FieldError error = Assert.Single(failure.FieldErrors);
        Assert.Equal("username", error.Field);
        Assert.Equal(message, error.Message);
    }
}
