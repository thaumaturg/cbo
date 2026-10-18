using System;
using Cbo.Results;

namespace Cbo.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_is_success_and_reading_its_failure_throws()
    {
        Result result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Failure);
    }

    [Fact]
    public void Failure_converts_implicitly_to_a_failed_result_with_the_same_kind_and_message()
    {
        Result result = Failure.NotFound("Tournament not found.");

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.NotFound, result.Failure.Kind);
        Assert.Equal("Tournament not found.", result.Failure.Message);
    }

    [Fact]
    public void Failure_converts_implicitly_to_a_failed_result_with_the_same_field_errors()
    {
        Result result = Failure.Invalid(
            "The round is invalid.",
            new FieldError("numberInMatch", "The round number is out of range."),
            new FieldError("topicId", "The topic does not exist."));

        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.Invalid, result.Failure.Kind);
        Assert.Equal("The round is invalid.", result.Failure.Message);
        Assert.Collection(
            result.Failure.FieldErrors,
            error =>
            {
                Assert.Equal("numberInMatch", error.Field);
                Assert.Equal("The round number is out of range.", error.Message);
            },
            error =>
            {
                Assert.Equal("topicId", error.Field);
                Assert.Equal("The topic does not exist.", error.Message);
            });
    }

    [Fact]
    public void A_null_failure_is_rejected()
    {
        Failure? missing = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            Result result = missing!;
        });
    }
}
