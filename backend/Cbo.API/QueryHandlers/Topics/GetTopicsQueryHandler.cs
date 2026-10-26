using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Topics;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.QueryHandlers.Topics;

public sealed class GetTopicsQueryHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : IQueryHandler<GetTopicsQuery, List<GetTopicDto>>
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<List<GetTopicDto>>> HandleAsync(GetTopicsQuery query, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.Topics
            .Where(t => t.TopicAuthors.Any(ta => ta.ApplicationUserId == currentUserId && ta.IsOwner))
            .Select(TopicProjections.ToGetDto(currentUserId))
            .ToListAsync(cancellationToken);
    }
}
