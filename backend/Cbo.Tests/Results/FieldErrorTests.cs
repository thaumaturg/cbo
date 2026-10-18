using System;
using Cbo.Results;

namespace Cbo.Tests.Results;

public class FieldErrorTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Rejects_an_empty_or_whitespace_field(string field)
    {
        Assert.Throws<ArgumentException>(() => new FieldError(field, "The topic does not exist."));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Rejects_an_empty_or_whitespace_message(string message)
    {
        Assert.Throws<ArgumentException>(() => new FieldError("topicId", message));
    }
}
