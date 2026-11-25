using Cbo.Core.Paging;
using Cbo.Core.Validation;

namespace Cbo.Core.Topics;

public sealed class GetTopicsRequestValidator : RequestValidator<GetTopicsRequest>
{
    public GetTopicsRequestValidator()
    {
        RuleFor(r => r.Page).Page();
        RuleFor(r => r.PageSize).PageSize();
        RuleFor(r => r.Sort).Sort(TopicListFilter.SortKeys);
        RuleFor(r => r.Search).Search();
    }

    public override string InvalidMessage => PagingRules.InvalidMessage;
}
