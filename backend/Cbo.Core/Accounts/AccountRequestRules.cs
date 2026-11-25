using Cbo.Core.Domain;
using FluentValidation;

namespace Cbo.Core.Accounts;

/// <summary>
/// Rules for the fields that the account request validators share. Passwords aren't trimmed, so a password is
/// missing only when it is empty; whitespace counts as characters.
/// </summary>
internal static class AccountRequestRules
{
    public static IRuleBuilderOptions<T, string> PasswordEntered<T>(this IRuleBuilder<T, string> rule, string message) =>
        rule
            .Must(password => !string.IsNullOrEmpty(password))
            .WithMessage(message);

    public static IRuleBuilderOptions<T, string> PasswordLength<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Length(Limits.Accounts.PasswordLengthMin, Limits.Accounts.PasswordLengthMax)
            .WithMessage(
                $"The password must be between {Limits.Accounts.PasswordLengthMin} and {Limits.Accounts.PasswordLengthMax} characters.");
}
