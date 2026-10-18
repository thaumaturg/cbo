using System;
using System.Diagnostics.CodeAnalysis;

namespace Cbo.Results;

public sealed class Result
{
    private readonly Failure? _failure;

    private Result(Failure? failure)
    {
        _failure = failure;
    }

    [MemberNotNullWhen(false, nameof(Failure))]
    public bool IsSuccess => _failure is null;

    [MemberNotNullWhen(true, nameof(Failure))]
    public bool IsFailure => _failure is not null;

    public Failure? Failure => _failure ?? throw new InvalidOperationException("Cannot read the failure of a successful result.");

    public static Result Success() => new(null);

    public static implicit operator Result(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        return new(failure);
    }
}
