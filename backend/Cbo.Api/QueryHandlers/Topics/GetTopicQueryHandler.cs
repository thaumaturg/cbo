using Cbo.Api.Authorization;
using Cbo.Api.Contracts.Topics;
using Cbo.Api.Cqrs;
using Cbo.Api.Data;
using Cbo.Api.Mappings;
using Cbo.Api.Models.Domain;
using Cbo.Api.Queries.Topics;
using Cbo.Api.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Api.QueryHandlers.Topics;

public sealed class GetTopicQueryHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService,
    ICurrentUserService currentUserService) : IQueryHandler<GetTopicQuery, TopicResponse>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this topic.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITopicAccessService _topicAccessService = topicAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<TopicResponse>> HandleAsync(GetTopicQuery query, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            query.TopicId, TopicOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.Topics
            .Where(t => t.Id == query.TopicId)
            .Select(TopicProjections.ToResponse(currentUserId))
            .SingleAsync(cancellationToken);
    }
}
