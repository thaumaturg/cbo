using Cbo.Api.Contracts.Topics;
using Cbo.Api.Cqrs;
using Cbo.Api.Data;
using Cbo.Api.Mappings;
using Cbo.Api.Queries.Topics;
using Cbo.Api.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Api.QueryHandlers.Topics;

public sealed class GetTopicsQueryHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : IQueryHandler<GetTopicsQuery, List<TopicResponse>>
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<List<TopicResponse>>> HandleAsync(GetTopicsQuery query, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.Topics
            .Where(t => t.TopicAuthors.Any(ta => ta.ApplicationUserId == currentUserId && ta.IsOwner))
            .Select(TopicProjections.ToResponse(currentUserId))
            .ToListAsync(cancellationToken);
    }
}
