namespace Cbo.Results;

/// <summary>
/// Composition helpers for <see cref="Result"/>.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Collapses many results into one. Returns <see cref="Result.Ok()"/> when all succeed; otherwise a failure with
    /// <paramref name="failureStatus"/> carrying every error from every failed result.
    /// </summary>
    public static Result ToAggregateResult(this IEnumerable<Result> results, ResultStatus failureStatus = ResultStatus.Invalid)
    {
        ArgumentNullException.ThrowIfNull(results);

        List<Error>? errors = null;

        foreach (Result result in results)
        {
            if (result.IsSuccess)
            {
                continue;
            }

            errors ??= [];
            errors.AddRange(result.Errors);
        }

        return errors is null ? Result.Ok() : Result.Failure(failureStatus, errors);
    }

    /// <summary>
    /// Groups <see cref="Result.Errors"/> by <see cref="Error.Identifier"/> — the shape of
    /// <c>ValidationProblemDetails.Errors</c>.
    /// </summary>
    public static Dictionary<string, string[]> ToErrorDictionary(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Errors
            .GroupBy(e => e.Identifier)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
    }
}
