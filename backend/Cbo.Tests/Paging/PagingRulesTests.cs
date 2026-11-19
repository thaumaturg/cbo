using System;
using System.Collections.Generic;
using Cbo.Core.Paging;
using Cbo.Results;

namespace Cbo.Tests.Paging;

public class PagingRulesTests
{
    private sealed record Row(Guid Id, string Title);

    private static readonly SortKeys<Row> SortKeys = new SortKeys<Row>("title", r => r.Id).Add("title", r => r.Title);

    [Theory]
    [InlineData(1, 1, null)]
    [InlineData(99, 100, "-title")]
    public void Valid_parameters_give_no_errors(int page, int pageSize, string? sort)
    {
        Assert.Empty(PagingRules.Validate(page, pageSize, sort, SortKeys));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_page_below_1_is_a_page_error(int page)
    {
        FieldError error = Assert.Single(PagingRules.Validate(page, 25, null, SortKeys));

        Assert.Equal("page", error.Field);
        Assert.Equal("The page must be 1 or greater.", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void A_page_size_outside_1_to_100_is_a_page_size_error(int pageSize)
    {
        FieldError error = Assert.Single(PagingRules.Validate(1, pageSize, null, SortKeys));

        Assert.Equal("pageSize", error.Field);
        Assert.Equal("The page size must be between 1 and 100.", error.Message);
    }

    [Fact]
    public void An_unknown_sort_is_a_sort_error_listing_the_allowed_keys()
    {
        FieldError error = Assert.Single(PagingRules.Validate(1, 25, "bogus", SortKeys));

        Assert.Equal("sort", error.Field);
        Assert.Equal("Unknown sort key. Allowed: title.", error.Message);
    }

    [Fact]
    public void Every_invalid_parameter_gets_its_own_error()
    {
        List<FieldError> errors = PagingRules.Validate(0, 0, "bogus", SortKeys);

        Assert.Equal(["page", "pageSize", "sort"], errors.ConvertAll(e => e.Field));
    }
}
