using Cbo.Results;

namespace Cbo.Api.ErrorHandling;

public static class FailureHttpExtensions
{
    /// <summary>
    /// The single place where a <see cref="Failure"/> becomes an HTTP problem details response.
    /// Field errors keep the status of the failure kind, so <c>TypedResults.ValidationProblem</c> (always 400)
    /// isn't used. <c>ProblemHttpResult</c> writes through <c>IProblemDetailsService</c>, which fills in
    /// <c>type</c>, <c>title</c> and <c>traceId</c>.
    /// </summary>
    public static IResult ToProblem(this Failure failure)
    {
        int statusCode = failure.Kind switch
        {
            FailureKind.Invalid => StatusCodes.Status400BadRequest,
            FailureKind.NotFound => StatusCodes.Status404NotFound,
            FailureKind.Conflict => StatusCodes.Status409Conflict,
            FailureKind.Forbidden => StatusCodes.Status403Forbidden,
            FailureKind.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => throw new InvalidOperationException($"Unknown failure kind '{failure.Kind}'."),
        };

        if (failure.FieldErrors.Count == 0)
            return TypedResults.Problem(detail: failure.Message, statusCode: statusCode);

        Dictionary<string, string[]> errors = failure.FieldErrors
            .GroupBy(fieldError => fieldError.Field)
            .ToDictionary(group => group.Key, group => group.Select(fieldError => fieldError.Message).ToArray());

        var problemDetails = new HttpValidationProblemDetails(errors)
        {
            Status = statusCode,
            Detail = failure.Message,
        };

        return TypedResults.Problem(problemDetails);
    }
}
