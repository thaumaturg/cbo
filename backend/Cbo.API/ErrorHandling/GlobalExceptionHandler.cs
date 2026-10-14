using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.ErrorHandling;

/// <summary>
/// Turns any unhandled exception into a logged 500 problem details response.
/// Exception details are included only in Development.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;
    private readonly IHostEnvironment _environment = environment;
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Same value the problem details defaults write to the "traceId" field, so a user report matches the log.
        string traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
        };

        if (_environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // The exception is logged and the status set, so it counts as handled even if the client's
        // Accept header rules out a JSON body. Returning false would make the middleware rethrow it.
        await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });

        return true;
    }
}
