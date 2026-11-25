using Cbo.Core.Validation;

namespace Cbo.Tests.Validation;

public class RequestTextTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(" \t Quiz night \n", "Quiz night")]
    [InlineData("Quiz  night", "Quiz  night")]
    public void Trim_removes_outer_whitespace_keeps_interior_whitespace_and_leaves_null_as_null(string? value, string? expected)
    {
        Assert.Equal(expected, RequestText.Trim(value));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" \t\n ", null)]
    [InlineData("  Quiz night ", "Quiz night")]
    [InlineData("Quiz \t night", "Quiz \t night")]
    public void TrimToNull_trims_and_turns_blank_into_null(string? value, string? expected)
    {
        Assert.Equal(expected, RequestText.TrimToNull(value));
    }
}
