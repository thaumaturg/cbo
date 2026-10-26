using Cbo.API.Authorization;
using Cbo.API.Commands.Topics;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Topics;

/// <summary>
/// Deletes a topic that hasn't been played. Its questions, authors and tournament choices go with it (database cascade).
/// </summary>
public sealed class DeleteTopicCommandHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService) : CommandHandler<DeleteTopicCommand>(dbContext)
{
    private const string DeleteForbiddenMessage = "You don't have permission to delete this topic.";

    private readonly ITopicAccessService _topicAccessService = topicAccessService;

    protected override async Task<Result> ExecuteAsync(DeleteTopicCommand command, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            command.TopicId, TopicOperations.Delete, DeleteForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        if (await TopicRules.IsPlayedAsync(DbContext, command.TopicId, cancellationToken))
            return PlayedCannotBeDeleted();

        DbContext.Topics.Remove(access.Value);

        return Result.Success();
    }

    protected override Failure? MapSaveFailure(DeleteTopicCommand command, DbUpdateException exception)
    {
        // Lost a race with a round that started using the topic after the check above
        if (PostgresConstraint.IsForeignKeyViolation(exception))
            return PlayedCannotBeDeleted();

        return null;
    }

    private static Failure PlayedCannotBeDeleted() =>
        Failure.Conflict("This topic has been played in a match, so it can't be deleted.");
}
