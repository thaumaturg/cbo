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
    [InlineData(PagingDefaults.FirstPage, 1, null)]
    [InlineData(99, PagingDefaults.MaxPageSize, "-title")]
    public void Valid_parameters_give_no_errors(int page, int pageSize, string? sort)
    {
        Assert.Empty(PagingRules.Validate(page, pageSize, sort, null, SortKeys));
    }

    [Theory]
    [InlineData(PagingDefaults.FirstPage - 1)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_page_before_the_first_is_a_page_error(int page)
    {
        FieldError error = Assert.Single(PagingRules.Validate(page, PagingDefaults.DefaultPageSize, null, null, SortKeys));

        Assert.Equal("page", error.Field);
        Assert.Equal($"The page must be {PagingDefaults.FirstPage} or greater.", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(PagingDefaults.MaxPageSize + 1)]
    public void A_page_size_outside_1_to_the_max_is_a_page_size_error(int pageSize)
    {
        FieldError error = Assert.Single(PagingRules.Validate(PagingDefaults.FirstPage, pageSize, null, null, SortKeys));

        Assert.Equal("pageSize", error.Field);
        Assert.Equal($"The page size must be between 1 and {PagingDefaults.MaxPageSize}.", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("history")]
    public void A_missing_or_short_search_gives_no_errors(string? search)
    {
        Assert.Empty(PagingRules.Validate(PagingDefaults.FirstPage, PagingDefaults.DefaultPageSize, null, search, SortKeys));
    }

    [Fact]
    public void A_search_of_the_max_length_after_trimming_gives_no_errors()
    {
        string search = $"  {new string('a', PagingDefaults.MaxSearchLength)}  ";

        Assert.Empty(PagingRules.Validate(PagingDefaults.FirstPage, PagingDefaults.DefaultPageSize, null, search, SortKeys));
    }

    [Fact]
    public void A_search_longer_than_the_max_length_after_trimming_is_a_search_error()
    {
        FieldError error = Assert.Single(PagingRules.Validate(
            PagingDefaults.FirstPage, PagingDefaults.DefaultPageSize, null,
            new string('a', PagingDefaults.MaxSearchLength + 1), SortKeys));

        Assert.Equal("search", error.Field);
        Assert.Equal($"The search can be at most {PagingDefaults.MaxSearchLength} characters.", error.Message);
    }

    [Fact]
    public void An_unknown_sort_is_a_sort_error_listing_the_allowed_keys()
    {
        FieldError error = Assert.Single(PagingRules.Validate(
            PagingDefaults.FirstPage, PagingDefaults.DefaultPageSize, "bogus", null, SortKeys));

        Assert.Equal("sort", error.Field);
        Assert.Equal("Unknown sort key. Allowed: title.", error.Message);
    }

    [Fact]
    public void Every_invalid_parameter_gets_its_own_error()
    {
        List<FieldError> errors = PagingRules.Validate(
            PagingDefaults.FirstPage - 1, 0, "bogus", new string('a', PagingDefaults.MaxSearchLength + 1), SortKeys);

        Assert.Equal(["page", "pageSize", "sort", "search"], errors.ConvertAll(e => e.Field));
    }
}
