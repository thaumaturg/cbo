using Cbo.Core.Paging;

namespace Cbo.Tests.Paging;

public class LikePatternTests
{
    [Fact]
    public void Contains_trims_the_term_and_wraps_it_in_wildcards()
    {
        Assert.Equal("%history%", LikePattern.Contains("  history \t"));
    }

    [Theory]
    [InlineData("100%", @"%100\%%")]
    [InlineData("snake_case", @"%snake\_case%")]
    [InlineData(@"C:\temp", @"%C:\\temp%")]
    [InlineData(@"\%_", @"%\\\%\_%")]
    public void Contains_escapes_wildcards_and_the_escape_character_so_they_match_themselves(string term, string expected)
    {
        Assert.Equal(expected, LikePattern.Contains(term));
    }

    [Fact]
    public void The_escape_character_is_a_backslash()
    {
        Assert.Equal(@"\", LikePattern.EscapeCharacter);
    }
}
