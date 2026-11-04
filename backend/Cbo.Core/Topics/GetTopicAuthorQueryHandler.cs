using Cbo.Core.Authorization;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Models.Domain;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Topics;

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
