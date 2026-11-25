using Cbo.Core.Topics;
using Cbo.Core.Validation;
using Cbo.Tests.Paging;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Topics;

public class GetTopicsRequestValidatorTests : PagingRequestRulesTests<GetTopicsRequest>
{
    protected override RequestValidator<GetTopicsRequest> Validator { get; } = new GetTopicsRequestValidator();

    protected override string KnownSortKey => "questionCount";

    protected override GetTopicsRequest Request(int? page = null, int? pageSize = null, string? sort = null, string? search = null) =>
        new() { Page = page, PageSize = pageSize, Sort = sort, Search = search };

    protected override string? SearchOf(GetTopicsRequest request) => request.Search;

    [Fact]
    public void An_unknown_sort_is_a_sort_error_listing_the_topic_sort_keys()
    {
        Validator.TestValidate(Request(sort: "stage"))
            .ShouldHaveValidationErrorFor("sort")
            .WithErrorMessage("Unknown sort key. Allowed: title, questionCount, createdAt.")
            .Only();
    }
}
