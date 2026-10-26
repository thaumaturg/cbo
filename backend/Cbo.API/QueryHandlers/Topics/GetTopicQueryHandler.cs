using Cbo.API.Authorization;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Topics;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.QueryHandlers.Topics;

public sealed class GetTopicQueryHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService,
    ICurrentUserService currentUserService) : IQueryHandler<GetTopicQuery, GetTopicDto>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this topic.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITopicAccessService _topicAccessService = topicAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<GetTopicDto>> HandleAsync(GetTopicQuery query, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            query.TopicId, TopicOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.Topics
            .Where(t => t.Id == query.TopicId)
            .Select(TopicProjections.ToGetDto(currentUserId))
            .SingleAsync(cancellationToken);
    }
}
