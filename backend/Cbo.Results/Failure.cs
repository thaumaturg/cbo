using System;
using System.Collections.Generic;

namespace Cbo.Results;

public sealed class Failure
{
    private Failure(FailureKind kind, string message, IEnumerable<FieldError> fieldErrors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Kind = kind;
        Message = message;
        FieldErrors = [.. fieldErrors];
    }

    public FailureKind Kind { get; }

    public string Message { get; }

    public IReadOnlyList<FieldError> FieldErrors { get; }

    public static Failure Invalid(string message, params IEnumerable<FieldError> fieldErrors) =>
        new(FailureKind.Invalid, message, fieldErrors);

    public static Failure NotFound(string message, params IEnumerable<FieldError> fieldErrors) =>
        new(FailureKind.NotFound, message, fieldErrors);

    public static Failure Conflict(string message, params IEnumerable<FieldError> fieldErrors) =>
        new(FailureKind.Conflict, message, fieldErrors);

    public static Failure Forbidden(string message, params IEnumerable<FieldError> fieldErrors) =>
        new(FailureKind.Forbidden, message, fieldErrors);

    public static Failure Unauthorized(string message, params IEnumerable<FieldError> fieldErrors) =>
        new(FailureKind.Unauthorized, message, fieldErrors);
}
