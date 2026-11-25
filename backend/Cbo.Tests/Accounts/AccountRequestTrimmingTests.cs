using System.Text.Json;
using Cbo.Core.Accounts;
using Cbo.Core.Domain;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Accounts;

/// <summary>
/// Account requests trim their text as the JSON body is read, so the validators see the value that gets saved.
/// Passwords are never trimmed.
/// </summary>
public class AccountRequestTrimmingTests
{
    [Fact]
    public void A_register_request_trims_its_username_email_and_full_name()
    {
        RegisterRequest request = Read<RegisterRequest>(
            """{ "email": " carol@example.com\t", "username": "  carol ", "password": "x", "fullName": " Carol  Smith \n" }""");

        Assert.Equal("carol@example.com", request.Email);
        Assert.Equal("carol", request.Username);
        Assert.Equal("Carol  Smith", request.FullName);
    }

    [Fact]
    public void A_blank_full_name_becomes_null()
    {
        RegisterRequest request = Read<RegisterRequest>(
            """{ "email": "carol@example.com", "username": "carol", "password": "x", "fullName": " \t " }""");

        Assert.Null(request.FullName);
    }

    [Fact]
    public void A_blank_full_name_is_not_an_error()
    {
        RegisterRequest request = Read<RegisterRequest>(
            """{ "email": "carol@example.com", "username": "carol", "password": "carol-password", "fullName": " " }""");

        new RegisterRequestValidator().TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_padded_username_is_checked_at_its_trimmed_length()
    {
        string shortUsername = new('u', Limits.Accounts.UsernameLengthMin - 1);
        RegisterRequest request = Read<RegisterRequest>(
            $$"""{ "email": "carol@example.com", "username": "  {{shortUsername}}  ", "password": "carol-password" }""");

        new RegisterRequestValidator().TestValidate(request)
            .ShouldHaveValidationErrorFor("username")
            .WithErrorMessage(
                $"The username must be between {Limits.Accounts.UsernameLengthMin} and {Limits.Accounts.UsernameLengthMax} characters.")
            .Only();
    }

    [Fact]
    public void A_register_password_is_not_trimmed()
    {
        RegisterRequest request = Read<RegisterRequest>(
            """{ "email": "carol@example.com", "username": "carol", "password": " carol-password\t" }""");

        Assert.Equal(" carol-password\t", request.Password);
    }

    [Fact]
    public void A_login_request_trims_the_username_or_email_but_not_the_password()
    {
        LoginRequest request = Read<LoginRequest>("""{ "usernameOrEmail": "  alice\t", "password": " secret " }""");

        Assert.Equal("alice", request.UsernameOrEmail);
        Assert.Equal(" secret ", request.Password);
    }

    [Fact]
    public void A_change_password_request_trims_neither_password()
    {
        ChangePasswordRequest request = Read<ChangePasswordRequest>(
            """{ "currentPassword": " old-password ", "newPassword": "\tnew-password " }""");

        Assert.Equal(" old-password ", request.CurrentPassword);
        Assert.Equal("\tnew-password ", request.NewPassword);
    }

    private static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)!;
}
