using Cbo.Core.Accounts;
using Cbo.Core.Domain;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Accounts;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Invalid_message_is_about_the_login()
    {
        Assert.Equal("Some of the login data is invalid.", _validator.InvalidMessage);
    }

    [Fact]
    public void A_filled_in_login_is_valid()
    {
        _validator.TestValidate(new LoginRequest { UsernameOrEmail = "alice", Password = "alice-password" })
            .ShouldNotHaveAnyValidationErrors();
    }

    // Login reveals nothing about the account limits, so any non-empty value is checked against the accounts
    [Theory]
    [InlineData(1)]
    [InlineData(Limits.Accounts.PasswordLengthMax + 1)]
    public void A_login_has_no_length_rules(int length)
    {
        _validator.TestValidate(new LoginRequest { UsernameOrEmail = new string('a', length), Password = new string('p', length) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData(null)]
    public void A_blank_username_or_email_is_required(string? usernameOrEmail)
    {
        _validator.TestValidate(new LoginRequest { UsernameOrEmail = usernameOrEmail!, Password = "alice-password" })
            .ShouldHaveValidationErrorFor("usernameOrEmail")
            .WithErrorMessage("Enter your username or email.")
            .Only();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void An_empty_password_is_required(string? password)
    {
        _validator.TestValidate(new LoginRequest { UsernameOrEmail = "alice", Password = password! })
            .ShouldHaveValidationErrorFor("password")
            .WithErrorMessage("Enter your password.")
            .Only();
    }

    // Passwords aren't trimmed, and registration accepts one made of whitespace
    [Fact]
    public void A_whitespace_password_is_checked_against_the_account()
    {
        _validator.TestValidate(new LoginRequest { UsernameOrEmail = "alice", Password = " \t" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Both_empty_login_fields_are_reported_together()
    {
        TestValidationResult<LoginRequest> result =
            _validator.TestValidate(new LoginRequest { UsernameOrEmail = "", Password = "" });

        Assert.Collection(
            result.Errors,
            error =>
            {
                Assert.Equal("usernameOrEmail", error.PropertyName);
                Assert.Equal("Enter your username or email.", error.ErrorMessage);
            },
            error =>
            {
                Assert.Equal("password", error.PropertyName);
                Assert.Equal("Enter your password.", error.ErrorMessage);
            });
    }
}
