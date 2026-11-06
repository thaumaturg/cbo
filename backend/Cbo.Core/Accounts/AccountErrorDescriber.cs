using Microsoft.AspNetCore.Identity;

namespace Cbo.Core.Accounts;

/// <summary>
/// Rewords the Identity errors that reach users as full sentences that say how to fix the field.
/// </summary>
public class AccountErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = "Username can only contain Latin letters, numbers and underscores.",
    };

    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = "This username is already taken.",
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = "An account with this email already exists.",
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = "Enter a valid email address.",
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = $"Password must be at least {length} characters long.",
    };

    public override IdentityError PasswordMismatch() => new()
    {
        Code = nameof(PasswordMismatch),
        Description = "Current password is incorrect.",
    };
}
