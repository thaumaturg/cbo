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

public sealed class GetTopicAuthorsQueryHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService) : IQueryHandler<GetTopicAuthorsQuery, List<GetTopicAuthorDto>>
{
    private const string ManageAuthorsForbiddenMessage = "You don't have permission to manage the authors of this topic.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITopicAccessService _topicAccessService = topicAccessService;

    public async Task<Result<List<GetTopicAuthorDto>>> HandleAsync(GetTopicAuthorsQuery query, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            query.TopicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        return await _dbContext.TopicAuthors
            .Where(ta => ta.TopicId == query.TopicId)
            .OrderBy(ta => ta.Id)
            .Select(TopicAuthorProjections.ToGetDto())
            .ToListAsync(cancellationToken);
    }
}
