using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.ErrorHandling;

/// <summary>
/// Turns any unhandled exception into a logged 500 problem details response.
/// Exception details are included only in Development. A request the client aborted gets a bodiless 499 instead,
/// and a request body or parameter Minimal APIs couldn't bind gets a 400 problem response.
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

        // The request token reaches EF, so a client disconnect can surface as an exception. ExceptionHandlerMiddleware
        // already answers a plain OperationCanceledException with 499 before calling this; this covers anything else
        // thrown while the request was aborted (for example a wrapped cancellation). Nobody reads the response.
        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(exception, "Request aborted by the client. TraceId: {TraceId}", traceId);

            if (!httpContext.Response.HasStarted)
                httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;

            return true;
        }

        // Minimal APIs throw this (ThrowOnBadRequest) for malformed JSON, a missing required property or an unknown
        // enum value. The message names internal C# types, so it is shown only in Development.
        if (exception is BadHttpRequestException badRequest)
        {
            _logger.LogInformation(badRequest, "Bad request. TraceId: {TraceId}", traceId);

            var badRequestProblem = new ProblemDetails
            {
                Title = "The request is invalid.",
            };

            if (_environment.IsDevelopment())
                badRequestProblem.Detail = badRequest.Message;

            await WriteProblemAsync(httpContext, badRequest.StatusCode, badRequestProblem, exception);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);

        var problemDetails = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
        };

        if (_environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        await WriteProblemAsync(httpContext, StatusCodes.Status500InternalServerError, problemDetails, exception);
        return true;
    }

    private async Task WriteProblemAsync(
        HttpContext httpContext, int statusCode, ProblemDetails problemDetails, Exception exception)
    {
        problemDetails.Status = statusCode;
        httpContext.Response.StatusCode = statusCode;

        // The exception is logged and the status set, so it counts as handled even if the client's
        // Accept header rules out a JSON body. Returning false would make the middleware rethrow it.
        await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });
    }
}
