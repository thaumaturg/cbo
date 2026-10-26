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

public sealed class GetTopicAuthorQueryHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService) : IQueryHandler<GetTopicAuthorQuery, GetTopicAuthorDto>
{
    private const string ManageAuthorsForbiddenMessage = "You don't have permission to manage the authors of this topic.";
    private const string NotFoundMessage = "Author not found for this topic.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITopicAccessService _topicAccessService = topicAccessService;

    public async Task<Result<GetTopicAuthorDto>> HandleAsync(GetTopicAuthorQuery query, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            query.TopicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        GetTopicAuthorDto? author = await _dbContext.TopicAuthors
            .Where(ta => ta.Id == query.AuthorId && ta.TopicId == query.TopicId)
            .Select(TopicAuthorProjections.ToGetDto())
            .FirstOrDefaultAsync(cancellationToken);
        if (author is null)
            return Failure.NotFound(NotFoundMessage);

        return author;
    }
}
