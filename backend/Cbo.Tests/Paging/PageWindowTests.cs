using Cbo.Core.Paging;

namespace Cbo.Tests.Paging;

public class PageWindowTests
{
    [Fact]
    public void The_first_page_skips_nothing()
    {
        PageWindow window = PageWindow.For(page: 1, pageSize: 25, totalCount: 37);

        Assert.Equal(0, window.Skip);
        Assert.False(window.IsPastEnd);
    }

    [Fact]
    public void Page_three_of_size_25_skips_the_first_50_rows()
    {
        PageWindow window = PageWindow.For(page: 3, pageSize: 25, totalCount: 60);

        Assert.Equal(50, window.Skip);
        Assert.False(window.IsPastEnd);
    }

    [Theory]
    [InlineData(2, 25, 25)]
    [InlineData(99, 25, 37)]
    [InlineData(int.MaxValue, 100, 37)]
    [InlineData(int.MaxValue, 100, int.MaxValue)]
    public void A_page_past_the_last_row_is_past_the_end_without_overflowing(int page, int pageSize, int totalCount)
    {
        PageWindow window = PageWindow.For(page, pageSize, totalCount);

        Assert.True(window.IsPastEnd);
        Assert.Equal(totalCount, window.Skip);
    }

    [Fact]
    public void With_no_rows_the_first_page_is_already_past_the_end()
    {
        PageWindow window = PageWindow.For(page: 1, pageSize: 25, totalCount: 0);

        Assert.Equal(0, window.Skip);
        Assert.True(window.IsPastEnd);
    }
}
