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

public sealed class GetTopicAuthorsQueryHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService) : IQueryHandler<GetTopicAuthorsQuery, List<TopicAuthorResponse>>
{
    private const string ManageAuthorsForbiddenMessage = "You don't have permission to manage the authors of this topic.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITopicAccessService _topicAccessService = topicAccessService;

    public async Task<Result<List<TopicAuthorResponse>>> HandleAsync(GetTopicAuthorsQuery query, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            query.TopicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        return await _dbContext.TopicAuthors
            .Where(ta => ta.TopicId == query.TopicId)
            .OrderBy(ta => ta.Id)
            .Select(TopicAuthorProjections.ToResponse())
            .ToListAsync(cancellationToken);
    }
}
