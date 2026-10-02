using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Infrastructure;

/// <summary>
/// Last line of defence for exceptions that escape the pipeline. Expected failures are modelled with
/// <c>Cbo.Results.Result</c> and never reach this handler; anything that does is a bug or an outage,
/// so it is logged once and answered with a generic RFC 9457 problem that never exposes exception text.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    // Non-standard but widely used (nginx) status for "client closed the connection".
    private const int StatusClientClosedRequest = 499;

    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to answer and nothing to log.
            httpContext.Response.StatusCode = StatusClientClosedRequest;
            return true;
        }

        _logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            }
        });

        // The exception is handled even if no problem-details writer accepted the request's Accept header;
        // the 500 status is already set.
        return true;
    }
}
