using Cbo.API.Authorization;
using Cbo.API.Commands.Topics;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Topics;

public sealed class DeleteTopicAuthorCommandHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService) : CommandHandler<DeleteTopicAuthorCommand>(dbContext)
{
    private const string ManageAuthorsForbiddenMessage = "You don't have permission to manage the authors of this topic.";

    private readonly ITopicAccessService _topicAccessService = topicAccessService;

    protected override async Task<Result> ExecuteAsync(DeleteTopicAuthorCommand command, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            command.TopicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        TopicAuthor? author = await DbContext.TopicAuthors
            .FirstOrDefaultAsync(ta => ta.Id == command.AuthorId && ta.TopicId == command.TopicId, cancellationToken);
        if (author is null)
            return Failure.NotFound("Author not found for this topic.");

        if (author.IsOwner)
            return Failure.Invalid("The topic's owner can't be removed from its authors.");

        if (await TopicRules.IsPlayedAsync(DbContext, command.TopicId, cancellationToken))
            return TopicFailures.PlayedAuthorsCannotChange();

        DbContext.TopicAuthors.Remove(author);

        return Result.Success();
    }
}
