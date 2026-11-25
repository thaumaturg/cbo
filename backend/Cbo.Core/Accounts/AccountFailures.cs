using Cbo.Results;

namespace Cbo.Core.Accounts;

/// <summary>
/// Failures shared by <see cref="AccountService"/> and the account request validators.
/// </summary>
internal static class AccountFailures
{
    public const string InvalidRegistrationMessage = "Some of the registration data is invalid.";
    public const string InvalidLoginMessage = "Some of the login data is invalid.";
    public const string InvalidPasswordChangeMessage = "Some of the password data is invalid.";

    public static Failure InvalidRegistration(IEnumerable<FieldError> fieldErrors) =>
        Failure.Invalid(InvalidRegistrationMessage, fieldErrors);

    public static Failure AlreadyRegistered(IEnumerable<FieldError> duplicates) =>
        Failure.Conflict("An account with this username or email already exists.", duplicates);

    public static Failure InvalidPasswordChange(IEnumerable<FieldError> fieldErrors) =>
        Failure.Invalid(InvalidPasswordChangeMessage, fieldErrors);
}
