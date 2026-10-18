using Cbo.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cbo.API.ErrorHandling;

public static class ControllerBaseExtensions
{
    /// <summary>
    /// The single place where a <see cref="Failure"/> becomes an HTTP problem details response.
    /// Bodies are built through <c>ProblemDetailsFactory</c> (via <c>Problem</c> and <c>ValidationProblem</c>),
    /// so <c>type</c>, <c>title</c> and <c>traceId</c> are filled in like every other problem response.
    /// </summary>
    public static ActionResult Problem(this ControllerBase controller, Failure failure)
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
            return controller.Problem(detail: failure.Message, statusCode: statusCode);

        var modelState = new ModelStateDictionary();
        foreach (FieldError fieldError in failure.FieldErrors)
        {
            modelState.AddModelError(fieldError.Field, fieldError.Message);
        }

        return controller.ValidationProblem(detail: failure.Message, statusCode: statusCode, modelStateDictionary: modelState);
    }
}
