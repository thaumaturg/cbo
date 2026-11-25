using System.Diagnostics.CodeAnalysis;

namespace Cbo.Core.Validation;

/// <summary>
/// Trims request text in the request DTO setters, so validators see exactly the value that gets saved.
/// Interior whitespace is kept. Passwords are never trimmed; see
/// docs/adr/0011-request-validation-with-fluentvalidation-before-access-checks.md.
/// </summary>
public static class RequestText
{
    /// <summary>
    /// For required text: the value without leading and trailing whitespace. Null stays null.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Trim(string? value) => value?.Trim();

    /// <summary>
    /// For optional text: the trimmed value, or null when nothing is left after trimming.
    /// </summary>
    public static string? TrimToNull(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
