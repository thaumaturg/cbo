using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;

namespace Cbo.Core.Topics;

/// <summary>
/// Creates the topic with its questions, the current user as its owner, and its co-authors, unless the account
/// already owns as many topics as allowed.
/// </summary>
public sealed class CreateTopicCommandHandler(
    CboDbContext dbContext,
    ITopicValidationService topicValidationService,
    ICurrentUserService currentUserService,
    ILookupNormalizer lookupNormalizer) : CommandHandler<CreateTopicCommand, Guid>(dbContext)
{
    private readonly ITopicValidationService _topicValidationService = topicValidationService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILookupNormalizer _lookupNormalizer = lookupNormalizer;

    protected override async Task<Result<Guid>> ExecuteAsync(CreateTopicCommand command, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        List<CoAuthorValidationModel> coAuthors = await CoAuthorLookup.LookUpAsync(
            DbContext, _lookupNormalizer, command.CoAuthorUsernames, cancellationToken);

        List<FieldError> fieldErrors = _topicValidationService.ValidateQuestions(
            command.Questions
                .Select(q => new QuestionValidationModel(q.QuestionNumber, q.Cost, q.Text, q.Answer))
                .ToList());
        fieldErrors.AddRange(_topicValidationService.ValidateCoAuthors(coAuthors, currentUserId));
        if (fieldErrors.Count > 0)
            return TopicFailures.Invalid(fieldErrors);

        int ownedCount = await TopicRules.CountOwnedAsync(DbContext, currentUserId, cancellationToken);
        Failure? capFailure = TopicCountRules.CheckOwnedTopicsCap(ownedCount, addedCount: 1);
        if (capFailure is not null)
            return capFailure;

        // Set here rather than by the database default, so the id is known before the save
        var topic = new Topic
        {
            Id = Guid.CreateVersion7(),
            Title = command.Title,
            Description = command.Description,
        };

        foreach (NewQuestion question in command.Questions)
        {
            topic.Questions.Add(new Question
            {
                QuestionNumber = question.QuestionNumber,
                Cost = question.Cost,
                Text = question.Text,
                Answer = question.Answer,
                Comment = question.Comment,
                TopicId = topic.Id,
            });
        }

        topic.TopicAuthors.Add(new TopicAuthor
        {
            IsOwner = true,
            IsAuthor = command.IsAuthor,
            ApplicationUserId = currentUserId,
            TopicId = topic.Id,
        });

        // Validation guarantees every co-author resolved to an account
        foreach (CoAuthorValidationModel coAuthor in coAuthors)
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
