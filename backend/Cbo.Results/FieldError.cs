using System;

namespace Cbo.Results;

public sealed class FieldError
{
    public FieldError(string field, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Field = field;
        Message = message;
    }

    /// <summary>
    /// The camelCase JSON path of the field as it appears on the wire, for example <c>topicId</c> or <c>questions[0].text</c>.
    /// </summary>
    public string Field { get; }

    public string Message { get; }
}
