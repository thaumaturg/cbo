using System.Text.Json;
using Cbo.Core.Friends;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Friends;

public class AddFriendRequestValidatorTests
{
    private readonly AddFriendRequestValidator _validator = new();

    [Fact]
    public void Invalid_message_says_the_friend_was_not_added()
    {
        Assert.Equal("The friend couldn't be added.", _validator.InvalidMessage);
    }

    [Fact]
    public void A_username_is_valid()
    {
        _validator.TestValidate(new AddFriendRequest { Username = "bob" }).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData(null)]
    public void A_blank_username_is_required(string? username)
    {
        _validator.TestValidate(new AddFriendRequest { Username = username! })
            .ShouldHaveValidationErrorFor("username")
            .WithErrorMessage("Username is required.")
            .Only();
    }

    [Fact]
    public void The_username_is_trimmed()
    {
        AddFriendRequest request = JsonSerializer.Deserialize<AddFriendRequest>(
            """{ "username": "  bob\t" }""", JsonSerializerOptions.Web)!;

        Assert.Equal("bob", request.Username);
    }
}
