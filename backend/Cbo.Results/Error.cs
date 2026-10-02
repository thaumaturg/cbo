namespace Cbo.Results;

/// <summary>
/// A single failure detail attached to a <see cref="Result"/>.
/// </summary>
/// <param name="Identifier">
/// camelCase JSON member path of the offending input property (e.g. <c>"answers"</c>,
/// <c>"questions[2].text"</c>), or <see cref="string.Empty"/> for an operation-level error.
/// This is the key under which the error appears in <c>ValidationProblemDetails.Errors</c>.
/// </param>
/// <param name="Message">Human-readable message.</param>
/// <param name="Code">Optional stable machine-readable code for client-side branching or i18n.</param>
public sealed record Error(string Identifier, string Message, string? Code = null)
{
    /// <summary>Creates an operation-level error that is not bound to a specific input member.</summary>
    /// <param name="message">Human-readable message.</param>
    public Error(string message) : this(string.Empty, message)
    {
    }
}
