using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Core.Topics;

/// <summary>
/// Creates every imported topic with its questions and the current user as its owner, or none of them when the
/// account would own too many topics. <see cref="ImportTopicsRequestValidator"/> has already checked the topics.
/// </summary>
public sealed class ImportTopicsCommandHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : CommandHandler<ImportTopicsCommand, int>(dbContext)
{
    private readonly ICurrentUserService _currentUserService = currentUserService;

    protected override async Task<Result<int>> ExecuteAsync(ImportTopicsCommand command, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        int ownedCount = await TopicRules.CountOwnedAsync(DbContext, currentUserId, cancellationToken);
        Failure? capFailure = TopicCountRules.CheckOwnedTopicsCap(ownedCount, command.Topics.Count);
        if (capFailure is not null)
            return capFailure;

        foreach (ImportedTopic importedTopic in command.Topics)
        {
            Topic topic = TopicFactory.Create(
                importedTopic.Title, importedTopic.Description, importedTopic.Questions, currentUserId, command.IsAuthor);
            DbContext.Topics.Add(topic);
        }

        return command.Topics.Count;
    }
}
