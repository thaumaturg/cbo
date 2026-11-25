using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Accounts;

/// <summary>
/// Checks only that both fields are filled in, so login reveals nothing about the account limits.
/// </summary>
public sealed class LoginRequestValidator : RequestValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.UsernameOrEmail).NotEmpty().WithMessage("Enter your username or email.");
        RuleFor(r => r.Password).PasswordEntered("Enter your password.");
    }

    public override string InvalidMessage => AccountFailures.InvalidLoginMessage;
}
