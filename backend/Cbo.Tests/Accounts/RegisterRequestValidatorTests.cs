using Cbo.Core.Accounts;
using Cbo.Core.Domain;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Accounts;

public class RegisterRequestValidatorTests
{
    private static readonly string UsernameLengthMessage =
        $"The username must be between {Limits.Accounts.UsernameLengthMin} and {Limits.Accounts.UsernameLengthMax} characters.";

    private static readonly string PasswordLengthMessage =
        $"The password must be between {Limits.Accounts.PasswordLengthMin} and {Limits.Accounts.PasswordLengthMax} characters.";

    private static readonly string FullNameLengthMessage =
        $"The full name must be between {Limits.Accounts.FullNameLengthMin} and {Limits.Accounts.FullNameLengthMax} characters.";

    private const string FullNameCharactersMessage =
        "Full name can only contain Latin, Cyrillic and Latvian letters, spaces, hyphens and apostrophes.";

    private static readonly RegisterRequest ValidRequest = new()
    {
        Email = "carol@example.com",
        Username = "carol",
        Password = "carol-password",
        FullName = "Carol Smith",
    };

    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Invalid_message_is_about_the_registration()
    {
        Assert.Equal("Some of the registration data is invalid.", _validator.InvalidMessage);
    }

    [Fact]
    public void A_complete_registration_is_valid()
    {
        _validator.TestValidate(ValidRequest).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Every_invalid_field_is_reported()
    {
        TestValidationResult<RegisterRequest> result = _validator.TestValidate(
            new RegisterRequest { Email = "", Username = "", Password = "", FullName = "1" });

        Assert.Equal(4, result.Errors.Count);
        result.ShouldHaveValidationErrorFor("email").WithErrorMessage("Email is required.");
        result.ShouldHaveValidationErrorFor("username").WithErrorMessage("Username is required.");
        result.ShouldHaveValidationErrorFor("password").WithErrorMessage("Password is required.");
        result.ShouldHaveValidationErrorFor("fullName").WithErrorMessage(FullNameLengthMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData(null)]
    public void A_blank_email_is_required(string? email)
    {
        _validator.TestValidate(ValidRequest with { Email = email! })
            .ShouldHaveValidationErrorFor("email").WithErrorMessage("Email is required.").Only();
    }

    [Fact]
    public void The_email_can_be_as_long_as_the_maximum()
    {
        _validator.TestValidate(ValidRequest with { Email = EmailOfLength(Limits.Accounts.EmailLengthMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_email_one_over_the_maximum_is_too_long()
    {
        _validator.TestValidate(ValidRequest with { Email = EmailOfLength(Limits.Accounts.EmailLengthMax + 1) })
            .ShouldHaveValidationErrorFor("email")
            .WithErrorMessage($"The email can be at most {Limits.Accounts.EmailLengthMax} characters.")
            .Only();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData(null)]
    public void A_blank_username_is_required(string? username)
    {
        _validator.TestValidate(ValidRequest with { Username = username! })
            .ShouldHaveValidationErrorFor("username").WithErrorMessage("Username is required.").Only();
    }

    [Fact]
    public void The_username_can_be_as_short_as_the_minimum()
    {
        _validator.TestValidate(ValidRequest with { Username = new string('u', Limits.Accounts.UsernameLengthMin) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_username_one_under_the_minimum_is_too_short()
    {
        _validator.TestValidate(ValidRequest with { Username = new string('u', Limits.Accounts.UsernameLengthMin - 1) })
            .ShouldHaveValidationErrorFor("username").WithErrorMessage(UsernameLengthMessage).Only();
    }

    [Fact]
    public void The_username_can_be_as_long_as_the_maximum()
    {
        _validator.TestValidate(ValidRequest with { Username = new string('u', Limits.Accounts.UsernameLengthMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_username_one_over_the_maximum_is_too_long()
    {
        _validator.TestValidate(ValidRequest with { Username = new string('u', Limits.Accounts.UsernameLengthMax + 1) })
            .ShouldHaveValidationErrorFor("username").WithErrorMessage(UsernameLengthMessage).Only();
    }

    // Identity checks the username's characters, see AccountServiceTests
    [Fact]
    public void The_validator_leaves_the_username_characters_to_identity()
    {
        _validator.TestValidate(ValidRequest with { Username = "car@ol" }).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void An_empty_password_is_required(string? password)
    {
        _validator.TestValidate(ValidRequest with { Password = password! })
            .ShouldHaveValidationErrorFor("password").WithErrorMessage("Password is required.").Only();
    }

    // Passwords aren't trimmed, so whitespace counts as characters
    [Fact]
    public void A_whitespace_password_is_checked_by_its_length()
    {
        _validator.TestValidate(ValidRequest with { Password = "   " })
            .ShouldHaveValidationErrorFor("password").WithErrorMessage(PasswordLengthMessage).Only();
    }

    [Fact]
    public void The_password_can_be_as_short_as_the_minimum()
    {
        _validator.TestValidate(ValidRequest with { Password = new string('p', Limits.Accounts.PasswordLengthMin) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_password_one_under_the_minimum_is_too_short()
    {
        _validator.TestValidate(ValidRequest with { Password = new string('p', Limits.Accounts.PasswordLengthMin - 1) })
            .ShouldHaveValidationErrorFor("password").WithErrorMessage(PasswordLengthMessage).Only();
    }

    [Fact]
    public void The_password_can_be_as_long_as_the_maximum()
    {
        _validator.TestValidate(ValidRequest with { Password = new string('p', Limits.Accounts.PasswordLengthMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_password_one_over_the_maximum_is_too_long()
    {
        _validator.TestValidate(ValidRequest with { Password = new string('p', Limits.Accounts.PasswordLengthMax + 1) })
            .ShouldHaveValidationErrorFor("password").WithErrorMessage(PasswordLengthMessage).Only();
    }

    [Fact]
    public void The_full_name_is_optional()
    {
        _validator.TestValidate(ValidRequest with { FullName = null }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void The_full_name_can_be_as_short_as_the_minimum()
    {
        _validator.TestValidate(ValidRequest with { FullName = new string('f', Limits.Accounts.FullNameLengthMin) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_full_name_one_under_the_minimum_is_too_short()
    {
        _validator.TestValidate(ValidRequest with { FullName = new string('f', Limits.Accounts.FullNameLengthMin - 1) })
            .ShouldHaveValidationErrorFor("fullName").WithErrorMessage(FullNameLengthMessage).Only();
    }

    [Fact]
    public void The_full_name_can_be_as_long_as_the_maximum()
    {
        _validator.TestValidate(ValidRequest with { FullName = new string('f', Limits.Accounts.FullNameLengthMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_full_name_one_over_the_maximum_is_too_long()
    {
        _validator.TestValidate(ValidRequest with { FullName = new string('f', Limits.Accounts.FullNameLengthMax + 1) })
            .ShouldHaveValidationErrorFor("fullName").WithErrorMessage(FullNameLengthMessage).Only();
    }

    [Theory]
    [InlineData("Jānis Bērziņš")]
    [InlineData("Ģirts Ķēniņš Žagars Čakste Šņore Ļūļa Ūdris Ēriks Īsa")]
    [InlineData("Иван Петров")]
    [InlineData("Ёлкин Алёна")]
    [InlineData("Anna-Marie")]
    [InlineData("O'Brien")]
    [InlineData("O’Brien")]
    [InlineData("Anna  Marie")]
    public void A_full_name_with_allowed_characters_is_valid(string fullName)
    {
        _validator.TestValidate(ValidRequest with { FullName = fullName }).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Anna 2")]
    [InlineData("Anna\tMarie")]
    [InlineData("Anna\nMarie")]
    [InlineData("Anna_Marie")]
    [InlineData("Anna.Marie")]
    [InlineData("Ελένη")]
    public void A_full_name_with_other_characters_is_invalid(string fullName)
    {
        _validator.TestValidate(ValidRequest with { FullName = fullName })
            .ShouldHaveValidationErrorFor("fullName").WithErrorMessage(FullNameCharactersMessage).Only();
    }

    [Fact]
    public void A_full_name_that_is_too_long_is_not_checked_for_characters()
    {
        _validator.TestValidate(ValidRequest with { FullName = new string('1', Limits.Accounts.FullNameLengthMax + 1) })
            .ShouldHaveValidationErrorFor("fullName").WithErrorMessage(FullNameLengthMessage).Only();
    }

    private static string EmailOfLength(int length)
    {
        const string domain = "@example.com";
        return new string('e', length - domain.Length) + domain;
    }
}
