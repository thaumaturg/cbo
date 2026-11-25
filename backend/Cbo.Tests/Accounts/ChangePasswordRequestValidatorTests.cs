using Cbo.Core.Accounts;
using Cbo.Core.Domain;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Accounts;

public class ChangePasswordRequestValidatorTests
{
    private static readonly string PasswordLengthMessage =
        $"The password must be between {Limits.Accounts.PasswordLengthMin} and {Limits.Accounts.PasswordLengthMax} characters.";

    private static readonly ChangePasswordRequest ValidRequest = new()
    {
        CurrentPassword = "old-password",
        NewPassword = "new-password",
    };

    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void Invalid_message_is_about_the_password()
    {
        Assert.Equal("Some of the password data is invalid.", _validator.InvalidMessage);
    }

    [Fact]
    public void A_new_password_different_from_the_current_one_is_valid()
    {
        _validator.TestValidate(ValidRequest).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void An_empty_current_password_is_required(string? currentPassword)
    {
        _validator.TestValidate(ValidRequest with { CurrentPassword = currentPassword! })
            .ShouldHaveValidationErrorFor("currentPassword")
            .WithErrorMessage("Enter your current password.")
            .Only();
    }

    // Identity checks it against the account; the length limits apply only to new passwords
    [Fact]
    public void The_current_password_has_no_length_rules()
    {
        _validator.TestValidate(ValidRequest with { CurrentPassword = "p" }).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void An_empty_new_password_is_required(string? newPassword)
    {
        _validator.TestValidate(ValidRequest with { NewPassword = newPassword! })
            .ShouldHaveValidationErrorFor("newPassword")
            .WithErrorMessage("Enter a new password.")
            .Only();
    }

    [Fact]
    public void The_new_password_can_be_as_short_as_the_minimum()
    {
        _validator.TestValidate(ValidRequest with { NewPassword = new string('p', Limits.Accounts.PasswordLengthMin) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_new_password_one_under_the_minimum_is_too_short()
    {
        _validator.TestValidate(ValidRequest with { NewPassword = new string('p', Limits.Accounts.PasswordLengthMin - 1) })
            .ShouldHaveValidationErrorFor("newPassword").WithErrorMessage(PasswordLengthMessage).Only();
    }

    [Fact]
    public void The_new_password_can_be_as_long_as_the_maximum()
    {
        _validator.TestValidate(ValidRequest with { NewPassword = new string('p', Limits.Accounts.PasswordLengthMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_new_password_one_over_the_maximum_is_too_long()
    {
        _validator.TestValidate(ValidRequest with { NewPassword = new string('p', Limits.Accounts.PasswordLengthMax + 1) })
            .ShouldHaveValidationErrorFor("newPassword").WithErrorMessage(PasswordLengthMessage).Only();
    }

    [Fact]
    public void A_new_password_equal_to_the_current_one_is_invalid()
    {
        _validator.TestValidate(ValidRequest with { NewPassword = ValidRequest.CurrentPassword })
            .ShouldHaveValidationErrorFor("newPassword")
            .WithErrorMessage("New password must be different from the current password.")
            .Only();
    }

    // Passwords aren't trimmed, so surrounding whitespace makes them different
    [Fact]
    public void A_new_password_that_differs_only_in_whitespace_is_different()
    {
        _validator.TestValidate(ValidRequest with { NewPassword = ValidRequest.CurrentPassword + " " })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_new_password_that_differs_only_in_letter_case_is_different()
    {
        _validator.TestValidate(ValidRequest with { NewPassword = ValidRequest.CurrentPassword.ToUpperInvariant() })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_short_new_password_equal_to_the_current_one_gets_only_the_length_error()
    {
        _validator.TestValidate(new ChangePasswordRequest { CurrentPassword = "short", NewPassword = "short" })
            .ShouldHaveValidationErrorFor("newPassword").WithErrorMessage(PasswordLengthMessage).Only();
    }

    [Fact]
    public void Both_empty_passwords_are_reported_together()
    {
        TestValidationResult<ChangePasswordRequest> result =
            _validator.TestValidate(new ChangePasswordRequest { CurrentPassword = "", NewPassword = "" });

        Assert.Collection(
            result.Errors,
            error =>
            {
                Assert.Equal("currentPassword", error.PropertyName);
                Assert.Equal("Enter your current password.", error.ErrorMessage);
            },
            error =>
            {
                Assert.Equal("newPassword", error.PropertyName);
                Assert.Equal("Enter a new password.", error.ErrorMessage);
            });
    }
}
