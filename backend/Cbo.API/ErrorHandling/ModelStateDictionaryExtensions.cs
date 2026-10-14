using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cbo.API.ErrorHandling;

public static class ModelStateDictionaryExtensions
{
    /// <summary>
    /// Adds hand-built field errors to model state, so that <c>ValidationProblem(ModelState)</c> can build the
    /// response through <c>ProblemDetailsFactory</c> (which fills in <c>type</c>, <c>title</c> and <c>traceId</c>).
    /// </summary>
    public static void AddModelErrors(this ModelStateDictionary modelState, IReadOnlyDictionary<string, string[]> errors)
    {
        foreach ((string field, string[] messages) in errors)
        {
            foreach (string message in messages)
            {
                modelState.AddModelError(field, message);
            }
        }
    }
}
