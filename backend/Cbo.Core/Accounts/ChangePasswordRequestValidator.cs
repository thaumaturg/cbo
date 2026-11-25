using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Accounts;

/// <summary>
/// Whether the current password is right stays an Identity check in <see cref="AccountService"/>.
/// </summary>
public sealed class ChangePasswordRequestValidator : RequestValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(r => r.CurrentPassword).PasswordEntered("Enter your current password.");

        RuleFor(r => r.NewPassword)
            .PasswordEntered("Enter a new password.")
            .PasswordLength()
            .NotEqual(r => r.CurrentPassword)
            .WithMessage("New password must be different from the current password.");
    }

    public override string InvalidMessage => AccountFailures.InvalidPasswordChangeMessage;
}
