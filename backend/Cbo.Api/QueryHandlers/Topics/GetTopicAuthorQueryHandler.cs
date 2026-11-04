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

public sealed class GetTopicAuthorQueryHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService) : IQueryHandler<GetTopicAuthorQuery, TopicAuthorResponse>
{
    private const string ManageAuthorsForbiddenMessage = "You don't have permission to manage the authors of this topic.";
    private const string NotFoundMessage = "Author not found for this topic.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITopicAccessService _topicAccessService = topicAccessService;

    public async Task<Result<TopicAuthorResponse>> HandleAsync(GetTopicAuthorQuery query, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            query.TopicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        TopicAuthorResponse? author = await _dbContext.TopicAuthors
            .Where(ta => ta.Id == query.AuthorId && ta.TopicId == query.TopicId)
            .Select(TopicAuthorProjections.ToResponse())
            .FirstOrDefaultAsync(cancellationToken);
        if (author is null)
            return Failure.NotFound(NotFoundMessage);

        return author;
    }
}
