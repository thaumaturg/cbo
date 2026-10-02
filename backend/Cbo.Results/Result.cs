namespace Cbo.Results;

/// <summary>
/// The outcome of an operation: a <see cref="ResultStatus"/> plus zero or more <see cref="Error"/>s.
/// Immutable. Returned, never thrown. Use the static factories to create instances.
/// </summary>
public class Result
{
    private static readonly Result s_ok = new(ResultStatus.Ok, [], null);
    private static readonly Result s_noContent = new(ResultStatus.NoContent, [], null);

    private protected Result(ResultStatus status, IReadOnlyList<Error> errors, string? location)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (IsSuccessStatus(status) && errors.Count > 0)
        {
            throw new ArgumentException($"A result with success status '{status}' cannot carry errors.", nameof(errors));
        }

        Status = status;
        Errors = errors;
        Location = location;
    }

    /// <summary>The semantic outcome.</summary>
    public ResultStatus Status { get; }

    /// <summary>Failure details. Never null; always empty when <see cref="IsSuccess"/> is true.</summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>The URI of the created resource. Set only by <see cref="Created{T}(T, string)"/>.</summary>
    public string? Location { get; }

    /// <summary>True when <see cref="Status"/> is <see cref="ResultStatus.Ok"/>, <see cref="ResultStatus.Created"/> or <see cref="ResultStatus.NoContent"/>.</summary>
    public bool IsSuccess => IsSuccessStatus(Status);

    /// <summary>True when <see cref="IsSuccess"/> is false.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The untyped value for adapters; null for a non-generic result or any failure.</summary>
    internal virtual object? GetValue() => null;

    // ---- success factories ---------------------------------------------------

    /// <summary>A successful result with no value. Returns a shared instance.</summary>
    public static Result Ok() => s_ok;

    /// <summary>A successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Ok<T>(T value) => new(ResultStatus.Ok, [], null, value);

    /// <summary>A successful result indicating that a resource was created at <paramref name="location"/>.</summary>
    public static Result<T> Created<T>(T value, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        return new(ResultStatus.Created, [], location, value);
    }

    /// <summary>A successful result with nothing to return. Returns a shared instance.</summary>
    public static Result NoContent() => s_noContent;

    // ---- failure factories ---------------------------------------------------

    /// <summary>The input failed validation.</summary>
    public static Result Invalid(params Error[] errors) => Failure(ResultStatus.Invalid, errors);

    /// <summary>The input failed validation.</summary>
    public static Result Invalid(IEnumerable<Error> errors) => Failure(ResultStatus.Invalid, errors);

    /// <summary>The input failed validation.</summary>
    public static Result<T> Invalid<T>(params Error[] errors) => Failure<T>(ResultStatus.Invalid, errors);

    /// <summary>The input failed validation.</summary>
    public static Result<T> Invalid<T>(IEnumerable<Error> errors) => Failure<T>(ResultStatus.Invalid, errors);

    /// <summary>The target resource does not exist or is not visible to the caller.</summary>
    public static Result NotFound(params Error[] errors) => Failure(ResultStatus.NotFound, errors);

    /// <summary>The target resource does not exist or is not visible to the caller.</summary>
    public static Result<T> NotFound<T>(params Error[] errors) => Failure<T>(ResultStatus.NotFound, errors);

    /// <summary>The operation conflicts with the current state of the resource.</summary>
    public static Result Conflict(params Error[] errors) => Failure(ResultStatus.Conflict, errors);

    /// <summary>The operation conflicts with the current state of the resource.</summary>
    public static Result<T> Conflict<T>(params Error[] errors) => Failure<T>(ResultStatus.Conflict, errors);

    /// <summary>The caller is not authenticated.</summary>
    public static Result Unauthorized() => Failure(ResultStatus.Unauthorized, []);

    /// <summary>The caller is not authenticated.</summary>
    public static Result<T> Unauthorized<T>() => Failure<T>(ResultStatus.Unauthorized, []);

    /// <summary>The caller is authenticated but not permitted.</summary>
    public static Result Forbidden() => Failure(ResultStatus.Forbidden, []);

    /// <summary>The caller is authenticated but not permitted.</summary>
    public static Result<T> Forbidden<T>() => Failure<T>(ResultStatus.Forbidden, []);

    /// <summary>An infrastructure-level failure that is still a modelled outcome.</summary>
    public static Result Unexpected(params Error[] errors) => Failure(ResultStatus.Unexpected, errors);

    /// <summary>An infrastructure-level failure that is still a modelled outcome.</summary>
    public static Result<T> Unexpected<T>(params Error[] errors) => Failure<T>(ResultStatus.Unexpected, errors);

    /// <summary>A failure with an explicit status. Throws if <paramref name="status"/> is a success status.</summary>
    public static Result Failure(ResultStatus status, IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ThrowIfSuccessStatus(status);
        return new(status, errors.ToArray(), null);
    }

    /// <summary>A failure with an explicit status. Throws if <paramref name="status"/> is a success status.</summary>
    public static Result<T> Failure<T>(ResultStatus status, IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ThrowIfSuccessStatus(status);
        return new(status, errors.ToArray(), null, default);
    }

    // ---- transformation ------------------------------------------------------

    /// <summary>
    /// Re-types a failure so it can be returned from a method with a different <see cref="Result{T}"/> signature:
    /// <c>if (check.IsFailure) return check.As&lt;GetRoundDto&gt;();</c>.
    /// Returns <c>this</c> when it already is a <see cref="Result{T}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">This result is a success and not already a <see cref="Result{T}"/>; there is no value to carry.</exception>
    public Result<T> As<T>()
    {
        if (this is Result<T> typed)
        {
            return typed;
        }

        if (IsSuccess)
        {
            throw new InvalidOperationException(
                $"Cannot re-type a successful '{GetType().Name}' as 'Result<{typeof(T).Name}>': there is no value to carry. Use Result.Ok<T>(value) instead.");
        }

        return new Result<T>(Status, Errors, Location, default);
    }

    /// <summary>Runs <paramref name="onSuccess"/> or <paramref name="onFailure"/> and returns its value.</summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Result, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Errors.Count == 0)
        {
            return Status.ToString();
        }

        IEnumerable<string> parts = Errors.Select(e => e.Identifier.Length == 0 ? e.Message : $"{e.Identifier}: {e.Message}");
        return $"{Status}: {string.Join("; ", parts)}";
    }

    private static bool IsSuccessStatus(ResultStatus status)
        => status is ResultStatus.Ok or ResultStatus.Created or ResultStatus.NoContent;

    private static void ThrowIfSuccessStatus(ResultStatus status)
    {
        if (IsSuccessStatus(status))
        {
            throw new ArgumentException($"'{status}' is a success status; use Result.Ok, Result.Created or Result.NoContent.", nameof(status));
        }
    }
}

/// <summary>
/// The outcome of an operation that produces a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(ResultStatus status, IReadOnlyList<Error> errors, string? location, T? value)
        : base(status, errors, location)
    {
        _value = value;
    }

    /// <summary>The value. Only readable when <see cref="Result.IsSuccess"/> is true.</summary>
    /// <exception cref="InvalidOperationException">This result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read Value of a failed result. {this}");

    internal override object? GetValue() => IsSuccess ? _value : null;

    /// <summary>Projects the value when successful; propagates the failure otherwise. Preserves <see cref="Result.Status"/> and <see cref="Result.Location"/>.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? new Result<TOut>(Status, Errors, Location, map(Value)) : As<TOut>();
    }

    /// <summary>Chains another result-producing step when successful; propagates the failure otherwise.</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess ? bind(Value) : As<TOut>();
    }

    /// <summary>Runs <paramref name="onSuccess"/> with the value, or <paramref name="onFailure"/> with this result, and returns its value.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Result<T>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(this);
    }
}
