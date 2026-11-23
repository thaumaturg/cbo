using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Core.Topics;

/// <summary>
/// Creates every imported topic with its questions and the current user as its owner, or none of them: not when the
/// import is empty or too big, when any topic's questions are invalid, or when the account would own too many topics.
/// </summary>
public sealed class ImportTopicsCommandHandler(
    CboDbContext dbContext,
    ITopicValidationService topicValidationService,
    ICurrentUserService currentUserService) : CommandHandler<ImportTopicsCommand, int>(dbContext)
{
    private readonly ITopicValidationService _topicValidationService = topicValidationService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    protected override async Task<Result<int>> ExecuteAsync(ImportTopicsCommand command, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        IReadOnlyList<FieldError> sizeErrors = TopicCountRules.CheckImportSize(command.Topics.Count);
        if (sizeErrors.Count > 0)
            return TopicFailures.InvalidImportSize(sizeErrors[0]);

        List<FieldError> fieldErrors = [];
        for (int i = 0; i < command.Topics.Count; i++)
        {
            List<FieldError> questionErrors = _topicValidationService.ValidateQuestions(
                command.Topics[i].Questions
                    .Select(q => new QuestionValidationModel(q.QuestionNumber, q.Cost, q.Text, q.Answer))
                    .ToList());
            fieldErrors.AddRange(FieldErrorPaths.Prefix(questionErrors, $"topics[{i}]."));
        }

        if (fieldErrors.Count > 0)
            return TopicFailures.InvalidImport(fieldErrors);

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
