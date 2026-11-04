using Cbo.Core.Authorization;
using Cbo.Core.Contracts.Topics;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Mappings;
using Cbo.Core.Models.Domain;
using Cbo.Core.Queries.Topics;
using Cbo.Core.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.QueryHandlers.Topics;

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
