using Cbo.Core.Cqrs;
using Cbo.Core.Paging;

namespace Cbo.Core.Topics;

/// <summary>
/// One page of the topics the current user owns.
/// A null <paramref name="Search"/>, <paramref name="Played"/> or <paramref name="IsAuthor"/> means no filter.
/// </summary>
public sealed record GetTopicsQuery(
    int Page,
    int PageSize,
    string? Sort,
    string? Search,
    bool? Played,
    bool? IsAuthor) : IQuery<PageResponse<TopicListItemResponse>>;
