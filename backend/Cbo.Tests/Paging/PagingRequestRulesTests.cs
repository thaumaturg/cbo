using Cbo.Core.Paging;
using Cbo.Core.Validation;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Paging;

/// <summary>
/// The paging rules every list request shares, run through each list's validator.
/// </summary>
public abstract class PagingRequestRulesTests<TRequest> where TRequest : class
{
    protected abstract RequestValidator<TRequest> Validator { get; }

    protected abstract TRequest Request(int? page = null, int? pageSize = null, string? sort = null, string? search = null);

    protected abstract string? SearchOf(TRequest request);

    /// <summary>A sort key the list allows.</summary>
    protected abstract string KnownSortKey { get; }

    [Fact]
    public void Invalid_message_is_about_the_list_parameters()
    {
        Assert.Equal("Some of the list parameters are invalid.", Validator.InvalidMessage);
    }

    [Fact]
    public void Missing_parameters_are_valid()
    {
        Validator.TestValidate(Request()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void The_first_page_and_the_max_page_size_are_valid()
    {
        Validator.TestValidate(Request(page: PagingDefaults.FirstPage, pageSize: PagingDefaults.MaxPageSize))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_page_size_of_1_is_valid()
    {
        Validator.TestValidate(Request(pageSize: 1)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(PagingDefaults.FirstPage - 1)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_page_before_the_first_is_a_page_error(int page)
    {
        Validator.TestValidate(Request(page: page))
            .ShouldHaveValidationErrorFor("page")
            .WithErrorMessage("The page must be 1 or greater.")
            .Only();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(PagingDefaults.MaxPageSize + 1)]
    public void A_page_size_outside_1_to_the_max_is_a_page_size_error(int pageSize)
    {
        Validator.TestValidate(Request(pageSize: pageSize))
            .ShouldHaveValidationErrorFor("pageSize")
            .WithErrorMessage("The page size must be between 1 and 100.")
            .Only();
    }

    [Fact]
    public void A_known_sort_key_is_valid_in_either_direction()
    {
        Validator.TestValidate(Request(sort: KnownSortKey)).ShouldNotHaveAnyValidationErrors();
        Validator.TestValidate(Request(sort: $"-{KnownSortKey}")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_search_of_the_max_length_is_valid()
    {
        Validator.TestValidate(Request(search: new string('a', PagingDefaults.MaxSearchLength)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_padded_search_is_checked_at_its_trimmed_length()
    {
        Validator.TestValidate(Request(search: $"  {new string('a', PagingDefaults.MaxSearchLength)}\t "))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_search_longer_than_the_max_length_is_a_search_error()
    {
        Validator.TestValidate(Request(search: new string('a', PagingDefaults.MaxSearchLength + 1)))
            .ShouldHaveValidationErrorFor("search")
            .WithErrorMessage("The search can be at most 200 characters.")
            .Only();
    }

    [Fact]
    public void Every_invalid_parameter_gets_its_own_error()
    {
        TestValidationResult<TRequest> result = Validator.TestValidate(Request(
            page: PagingDefaults.FirstPage - 1,
            pageSize: 0,
            sort: "bogus",
            search: new string('a', PagingDefaults.MaxSearchLength + 1)));

        Assert.Equal(["page", "pageSize", "sort", "search"], result.Errors.ConvertAll(e => e.PropertyName));
    }

    [Fact]
    public void The_search_is_trimmed()
    {
        Assert.Equal("history  of art", SearchOf(Request(search: " \thistory  of art  ")));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \n\t ")]
    public void A_blank_search_becomes_null(string search)
    {
        Assert.Null(SearchOf(Request(search: search)));
    }
}
