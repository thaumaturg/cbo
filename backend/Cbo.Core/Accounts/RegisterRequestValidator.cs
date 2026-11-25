using Cbo.Core.Domain;
using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Accounts;

/// <summary>
/// The username's characters and the email's format stay Identity checks, see <see cref="AccountIdentityOptions"/>.
/// </summary>
public sealed class RegisterRequestValidator : RequestValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(Limits.Accounts.EmailLengthMax)
            .WithMessage($"The email can be at most {Limits.Accounts.EmailLengthMax} characters.");

        RuleFor(r => r.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(Limits.Accounts.UsernameLengthMin, Limits.Accounts.UsernameLengthMax)
            .WithMessage(
                $"The username must be between {Limits.Accounts.UsernameLengthMin} and {Limits.Accounts.UsernameLengthMax} characters.");

        RuleFor(r => r.Password)
            .PasswordEntered("Password is required.")
            .PasswordLength();

        RuleFor(r => r.FullName)
            .Length(Limits.Accounts.FullNameLengthMin, Limits.Accounts.FullNameLengthMax)
            .WithMessage(
                $"The full name must be between {Limits.Accounts.FullNameLengthMin} and {Limits.Accounts.FullNameLengthMax} characters.")
            .Matches(Limits.Accounts.FullNamePattern)
            .WithMessage("Full name can only contain Latin, Cyrillic and Latvian letters, spaces, hyphens and apostrophes.");
    }

    public override string InvalidMessage => AccountFailures.InvalidRegistrationMessage;
}
