using System;
using Cbo.Api.ErrorHandling;
using Cbo.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cbo.Tests.ErrorHandling;

public class FailureHttpExtensionsTests
{
    [Theory]
    [InlineData(FailureKind.Invalid, 400)]
    [InlineData(FailureKind.NotFound, 404)]
    [InlineData(FailureKind.Conflict, 409)]
    [InlineData(FailureKind.Forbidden, 403)]
    [InlineData(FailureKind.Unauthorized, 401)]
    public void A_failure_without_field_errors_becomes_a_problem_with_its_kinds_status_and_the_message_as_detail(
        FailureKind kind, int expectedStatus)
    {
        Failure failure = CreateFailure(kind, "Something went wrong.");

        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(failure.ToProblem());

        Assert.Equal(expectedStatus, problem.StatusCode);
        Assert.Equal(expectedStatus, problem.ProblemDetails.Status);
        Assert.Equal("Something went wrong.", problem.ProblemDetails.Detail);
    }

    [Theory]
    [InlineData(FailureKind.Invalid, 400)]
    [InlineData(FailureKind.NotFound, 404)]
    [InlineData(FailureKind.Conflict, 409)]
    [InlineData(FailureKind.Forbidden, 403)]
    [InlineData(FailureKind.Unauthorized, 401)]
    public void A_failure_with_field_errors_becomes_a_validation_problem_that_keeps_its_kinds_status_and_groups_errors_by_field(
        FailureKind kind, int expectedStatus)
    {
        Failure failure = CreateFailure(
            kind,
            "The account could not be created.",
            new FieldError("email", "The email is taken."),
            new FieldError("username", "The username is taken."),
            new FieldError("email", "The email is invalid."));

        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(failure.ToProblem());

        Assert.Equal(expectedStatus, problem.StatusCode);
        HttpValidationProblemDetails details = Assert.IsType<HttpValidationProblemDetails>(problem.ProblemDetails);
        Assert.Equal(expectedStatus, details.Status);
        Assert.Equal("The account could not be created.", details.Detail);
        Assert.Equal(2, details.Errors.Count);
        Assert.Equal(["The email is taken.", "The email is invalid."], details.Errors["email"]);
        Assert.Equal(["The username is taken."], details.Errors["username"]);
    }

    private static Failure CreateFailure(FailureKind kind, string message, params FieldError[] fieldErrors) => kind switch
    {
        FailureKind.Invalid => Failure.Invalid(message, fieldErrors),
        FailureKind.NotFound => Failure.NotFound(message, fieldErrors),
        FailureKind.Conflict => Failure.Conflict(message, fieldErrors),
        FailureKind.Forbidden => Failure.Forbidden(message, fieldErrors),
        FailureKind.Unauthorized => Failure.Unauthorized(message, fieldErrors),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
