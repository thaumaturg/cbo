using System;
using System.Collections.Generic;
using Cbo.Results;

namespace Cbo.Tests.Results;

public class FailureTests
{
    [Fact]
    public void Each_factory_produces_its_kind()
    {
        Assert.Equal(FailureKind.Invalid, Failure.Invalid("Invalid.").Kind);
        Assert.Equal(FailureKind.NotFound, Failure.NotFound("Not found.").Kind);
        Assert.Equal(FailureKind.Conflict, Failure.Conflict("Conflict.").Kind);
        Assert.Equal(FailureKind.Forbidden, Failure.Forbidden("Forbidden.").Kind);
        Assert.Equal(FailureKind.Unauthorized, Failure.Unauthorized("Unauthorized.").Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Every_factory_rejects_an_empty_or_whitespace_message(string message)
    {
        Assert.Throws<ArgumentException>(() => Failure.Invalid(message));
        Assert.Throws<ArgumentException>(() => Failure.NotFound(message));
        Assert.Throws<ArgumentException>(() => Failure.Conflict(message));
        Assert.Throws<ArgumentException>(() => Failure.Forbidden(message));
        Assert.Throws<ArgumentException>(() => Failure.Unauthorized(message));
    }

    [Fact]
    public void Field_errors_cannot_be_changed_through_the_failure()
    {
        Failure failure = Failure.Invalid("The round is invalid.", new FieldError("topicId", "The topic does not exist."));
        FieldError replacement = new("answers", "The answers do not fit the scoring mode.");

        if (failure.FieldErrors is IList<FieldError> exposed)
        {
            Assert.Throws<NotSupportedException>(() => exposed[0] = replacement);
            Assert.Throws<NotSupportedException>(() => exposed.Add(replacement));
        }

        FieldError error = Assert.Single(failure.FieldErrors);
        Assert.Equal("topicId", error.Field);
    }

    [Fact]
    public void Changing_the_given_collection_afterwards_does_not_change_the_failure()
    {
        List<FieldError> given = [new FieldError("topicId", "The topic does not exist.")];
        Failure failure = Failure.Invalid("The round is invalid.", given);

        given.Add(new FieldError("answers", "The answers do not fit the scoring mode."));
        given[0] = new FieldError("numberInMatch", "The round number is out of range.");

        FieldError error = Assert.Single(failure.FieldErrors);
        Assert.Equal("topicId", error.Field);
    }

    [Fact]
    public void A_failure_without_field_errors_has_none()
    {
        Failure failure = Failure.NotFound("Tournament not found.");

        Assert.Empty(failure.FieldErrors);
    }
}
