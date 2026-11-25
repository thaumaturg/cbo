using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Paging;
using Cbo.Results;

namespace Cbo.Core.Topics;

public sealed class GetTopicsQueryHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : IQueryHandler<GetTopicsQuery, PageResponse<TopicListItemResponse>>
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<PageResponse<TopicListItemResponse>>> HandleAsync(GetTopicsQuery query, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await TopicListFilter.Apply(_dbContext.Topics, query, currentUserId)
            .ToPageAsync(
                TopicListFilter.SortKeys,
                query.Sort,
                query.Page,
                query.PageSize,
                TopicProjections.ToListItemResponse(currentUserId),
                cancellationToken);
    }
}
