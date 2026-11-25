using Cbo.Results;
using FluentValidation.Results;

namespace Cbo.Core.Validation;

public static class ValidationResultExtensions
{
    /// <summary>
    /// The failures as field errors, in the order the validator found them. The paths are already camelCase JSON
    /// paths (see <see cref="ValidationSetup.ConfigureGlobalOptions"/>).
    /// </summary>
    public static List<FieldError> ToFieldErrors(this ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return [.. result.Errors.Select(failure => new FieldError(failure.PropertyName, failure.ErrorMessage))];
    }
}
