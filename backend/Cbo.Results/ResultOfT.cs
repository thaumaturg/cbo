using System;
using System.Diagnostics.CodeAnalysis;

namespace Cbo.Results;

public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Failure? _failure;

    private Result(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _value = value;
    }

    private Result(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        _failure = failure;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Failure))]
    public bool IsSuccess => _failure is null;

    [MemberNotNullWhen(false, nameof(Value))]
    [MemberNotNullWhen(true, nameof(Failure))]
    public bool IsFailure => _failure is not null;

    public T? Value => IsSuccess ? _value : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public Failure? Failure => _failure ?? throw new InvalidOperationException("Cannot read the failure of a successful result.");

    public static Result<T> Success(T value) => new(value);

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Failure failure) => new(failure);
}
