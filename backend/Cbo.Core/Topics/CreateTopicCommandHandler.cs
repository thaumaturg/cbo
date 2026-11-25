using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;

namespace Cbo.Core.Topics;

/// <summary>
/// Creates the topic with its questions, the current user as its owner, and its co-authors, unless the account
/// already owns as many topics as allowed. <see cref="CreateTopicRequestValidator"/> has already checked the request;
/// this handler checks the co-authors' accounts.
/// </summary>
public sealed class CreateTopicCommandHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService,
    ILookupNormalizer lookupNormalizer) : CommandHandler<CreateTopicCommand, Guid>(dbContext)
{
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILookupNormalizer _lookupNormalizer = lookupNormalizer;

    protected override async Task<Result<Guid>> ExecuteAsync(CreateTopicCommand command, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        List<CoAuthorAccount> coAuthors = await CoAuthorLookup.LookUpAsync(
            DbContext, _lookupNormalizer, command.CoAuthorUsernames, cancellationToken);

        IReadOnlyList<FieldError> fieldErrors = CoAuthorRules.CheckAccounts(coAuthors, currentUserId);
        if (fieldErrors.Count > 0)
            return TopicFailures.Invalid(fieldErrors);

        int ownedCount = await TopicRules.CountOwnedAsync(DbContext, currentUserId, cancellationToken);
        Failure? capFailure = TopicCountRules.CheckOwnedTopicsCap(ownedCount, addedCount: 1);
        if (capFailure is not null)
            return capFailure;

        Topic topic = TopicFactory.Create(
            command.Title, command.Description, command.Questions, currentUserId, command.IsAuthor);

        // The checks above guarantee every co-author resolved to an account
        foreach (CoAuthorAccount coAuthor in coAuthors)
        {
            topic.TopicAuthors.Add(new TopicAuthor
            {
                IsOwner = false,
                IsAuthor = true,
                ApplicationUserId = coAuthor.UserId!.Value,
                TopicId = topic.Id,
            });
        }

        DbContext.Topics.Add(topic);

        return topic.Id;
    }
}
