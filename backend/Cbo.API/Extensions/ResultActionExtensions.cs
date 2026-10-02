using Cbo.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

namespace Cbo.API.Extensions;

/// <summary>
/// Projects a <see cref="Result"/> onto an MVC <see cref="IActionResult"/>.
/// This is the only place where <see cref="Result"/> meets HTTP.
/// </summary>
public static class ResultActionExtensions
{
    /// <summary>Problem-details extension member carrying the distinct <see cref="Error.Code"/>s, for client-side branching.</summary>
    public const string ErrorCodesExtension = "errorCodes";

    /// <summary>
    /// Converts <paramref name="result"/> to an <see cref="IActionResult"/>.
    /// Successes become 200/201/204; failures become RFC 9457 problem details built by
    /// <see cref="ControllerBase.ProblemDetailsFactory"/>, so they match the framework's own error responses:
    /// operation-level errors become <c>detail</c>, member-level errors become <c>errors</c>,
    /// and any <see cref="Error.Code"/>s are listed under <c>errorCodes</c>.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="controller">The executing controller (supplies <c>HttpContext</c> and <c>ProblemDetailsFactory</c>).</param>
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);

        if (result.IsFailure)
        {
            return ToProblemResult(result, controller);
        }

        object? value = result.GetValue();

        return result.Status switch
        {
            ResultStatus.Ok => value is null ? new OkResult() : new OkObjectResult(value),
            ResultStatus.Created => new CreatedResult(result.Location, value),
            ResultStatus.NoContent => new NoContentResult(),
            _ => throw new InvalidOperationException($"Unhandled success status '{result.Status}'.")
        };
    }

    private static ObjectResult ToProblemResult(Result result, ControllerBase controller)
    {
        int statusCode = ToStatusCode(result.Status);

        // Operation-level errors (empty Identifier) describe the request as a whole, which is what RFC 9457 "detail" is for.
        // Member-level errors go to the ValidationProblemDetails "errors" map so clients can bind them to form fields.
        string? detail = JoinMessages(result.Errors.Where(e => e.Identifier.Length == 0));
        List<Error> memberErrors = result.Errors.Where(e => e.Identifier.Length > 0).ToList();

        ProblemDetails problemDetails = memberErrors.Count > 0
            ? CreateValidationProblemDetails(controller, result.Status, statusCode, memberErrors, detail)
            : controller.ProblemDetailsFactory.CreateProblemDetails(controller.HttpContext, statusCode, detail: detail);

        string[] errorCodes = result.Errors
            .Select(e => e.Code)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (errorCodes.Length > 0)
        {
            problemDetails.Extensions[ErrorCodesExtension] = errorCodes;
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    private static ValidationProblemDetails CreateValidationProblemDetails(
        ControllerBase controller,
        ResultStatus status,
        int statusCode,
        List<Error> memberErrors,
        string? detail)
    {
        ModelStateDictionary modelState = new();
        foreach (Error error in memberErrors)
        {
            modelState.AddModelError(error.Identifier, error.Message);
        }

        // ValidationProblemDetails defaults its title to "One or more validation errors occurred.",
        // which is only right for Invalid. Use the reason phrase for every other status.
        string? title = status == ResultStatus.Invalid ? null : ReasonPhrases.GetReasonPhrase(statusCode);

        return controller.ProblemDetailsFactory.CreateValidationProblemDetails(
            controller.HttpContext,
            modelState,
            statusCode,
            title,
            detail: detail);
    }

    private static string? JoinMessages(IEnumerable<Error> errors)
    {
        string joined = string.Join(" ", errors.Select(e => e.Message));
        return joined.Length == 0 ? null : joined;
    }

    private static int ToStatusCode(ResultStatus status) => status switch
    {
        ResultStatus.Invalid => StatusCodes.Status400BadRequest,
        ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
        ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
        ResultStatus.NotFound => StatusCodes.Status404NotFound,
        ResultStatus.Conflict => StatusCodes.Status409Conflict,
        ResultStatus.Unexpected => StatusCodes.Status500InternalServerError,
        _ => throw new InvalidOperationException($"'{status}' is not a failure status.")
    };
}
