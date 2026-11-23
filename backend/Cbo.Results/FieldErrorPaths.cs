using System.Collections.Generic;
using System.Linq;

namespace Cbo.Results;

/// <summary>
/// Moves field errors under a parent path, for a request that nests a validated object inside a larger one.
/// </summary>
public static class FieldErrorPaths
{
    /// <summary>
    /// Returns the errors with <paramref name="prefix"/> put in front of each field, so <c>questions[0].text</c> under
    /// <c>topics[2].</c> becomes <c>topics[2].questions[0].text</c>. The prefix includes its trailing separator.
    /// </summary>
    public static IReadOnlyList<FieldError> Prefix(IEnumerable<FieldError> fieldErrors, string prefix) =>
        [.. fieldErrors.Select(e => new FieldError(prefix + e.Field, e.Message))];
}
