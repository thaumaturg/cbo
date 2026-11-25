using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Data.Configuration;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Topics;

/// <summary>
/// Updates the topic's details and syncs its questions and co-authors. Questions with an id are changed, questions
/// without one are added, and the topic's other questions are removed. Co-authors still listed are kept, new ones are
/// added and the rest are removed; the owner's row only gets its <see cref="TopicAuthor.IsAuthor"/> flag updated.
/// <see cref="UpdateTopicRequestValidator"/> has already checked the request; this handler checks what needs the
/// database: the question ids and the co-authors' accounts.
/// </summary>
public sealed class UpdateTopicCommandHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService,
    ICurrentUserService currentUserService,
    ILookupNormalizer lookupNormalizer) : CommandHandler<UpdateTopicCommand>(dbContext)
{
    private const string UpdateForbiddenMessage = "You don't have permission to edit this topic.";

    private readonly ITopicAccessService _topicAccessService = topicAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILookupNormalizer _lookupNormalizer = lookupNormalizer;

    protected override async Task<Result> ExecuteAsync(UpdateTopicCommand command, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            command.TopicId, TopicOperations.Update, UpdateForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Topic topic = access.Value;

        // Only the owner can update the topic, so the current user is its owner
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        List<Question> existingQuestions = await DbContext.Questions
            .Where(q => q.TopicId == command.TopicId)
            .ToListAsync(cancellationToken);

        List<CoAuthorAccount> coAuthors = await CoAuthorLookup.LookUpAsync(
            DbContext, _lookupNormalizer, command.CoAuthorUsernames, cancellationToken);

        List<FieldError> fieldErrors = [.. ValidateQuestionIds(existingQuestions, command.Questions)];
        fieldErrors.AddRange(CoAuthorRules.CheckAccounts(coAuthors, currentUserId));
        if (fieldErrors.Count > 0)
            return TopicFailures.Invalid(fieldErrors);

        if (await TopicRules.IsPlayedAsync(DbContext, command.TopicId, cancellationToken))
            return Failure.Conflict("This topic has been played in a match, so it can't be edited.");

        topic.Title = command.Title;
        topic.Description = command.Description;

        SyncQuestions(command.TopicId, existingQuestions, command.Questions);

        List<TopicAuthor> existingAuthors = await DbContext.TopicAuthors
            .Where(ta => ta.TopicId == command.TopicId)
            .ToListAsync(cancellationToken);

        TopicAuthor? owner = existingAuthors.FirstOrDefault(ta => ta.IsOwner);
        if (owner is not null)
            owner.IsAuthor = command.IsAuthor;

        // The checks above guarantee every co-author resolved to an account
        SyncCoAuthors(
            command.TopicId,
            existingAuthors.Where(ta => !ta.IsOwner).ToList(),
            coAuthors.Select(a => a.UserId!.Value).ToList());

        return Result.Success();
    }

    protected override Failure? MapSaveFailure(UpdateTopicCommand command, DbUpdateException exception)
    {
        // Lost a race with a simultaneous save that added the same co-author
        if (PostgresConstraint.IsUniqueViolation(exception, TopicAuthorConfiguration.TopicIdApplicationUserIdIndexName))
            return Failure.Conflict("Someone changed this topic's authors at the same time. Reload the page and try again.");

        return null;
    }

    /// <summary>
    /// Keeps the rows of co-authors still in <paramref name="coAuthorUserIds"/>, so their ids stay stable, removes the
    /// others and adds rows for the new ones. Never sees the owner's row.
    /// </summary>
    private void SyncCoAuthors(Guid topicId, List<TopicAuthor> existingCoAuthors, IReadOnlyList<Guid> coAuthorUserIds)
    {
        HashSet<Guid> keptUserIds = coAuthorUserIds.ToHashSet();
        DbContext.TopicAuthors.RemoveRange(existingCoAuthors.Where(ta => !keptUserIds.Contains(ta.ApplicationUserId)));

        HashSet<Guid> existingUserIds = existingCoAuthors.Select(ta => ta.ApplicationUserId).ToHashSet();
        foreach (Guid userId in coAuthorUserIds.Where(id => !existingUserIds.Contains(id)))
        {
            DbContext.TopicAuthors.Add(new TopicAuthor
            {
                IsOwner = false,
                IsAuthor = true,
                ApplicationUserId = userId,
                TopicId = topicId,
            });
        }
    }

    /// <summary>
    /// Every question id in the request must be a question of this topic, and appear at most once.
    /// </summary>
    private static IEnumerable<FieldError> ValidateQuestionIds(List<Question> existingQuestions, IReadOnlyList<EditedQuestion> questions)
    {
        HashSet<Guid> topicQuestionIds = existingQuestions.Select(q => q.Id).ToHashSet();
        HashSet<Guid> seenQuestionIds = [];

        for (int i = 0; i < questions.Count; i++)
        {
            if (questions[i].Id is not Guid questionId)
                continue;

            if (!topicQuestionIds.Contains(questionId))
                yield return new FieldError($"questions[{i}].id", "This question is not part of the topic.");
            else if (!seenQuestionIds.Add(questionId))
                yield return new FieldError($"questions[{i}].id", "This question appears more than once.");
        }
    }

    /// <summary>
    /// Requires every id in <paramref name="questions"/> to be one of <paramref name="existingQuestions"/>.
    /// </summary>
    private void SyncQuestions(Guid topicId, List<Question> existingQuestions, IReadOnlyList<EditedQuestion> questions)
    {
        HashSet<Guid> keptQuestionIds = questions
            .Where(q => q.Id.HasValue)
            .Select(q => q.Id!.Value)
            .ToHashSet();

        DbContext.Questions.RemoveRange(existingQuestions.Where(q => !keptQuestionIds.Contains(q.Id)));

        foreach (EditedQuestion question in questions)
        {
            if (question.Id is Guid questionId)
            {
                Question existingQuestion = existingQuestions.First(q => q.Id == questionId);
                existingQuestion.QuestionNumber = question.QuestionNumber;
                existingQuestion.Cost = question.Cost;
                existingQuestion.Text = question.Text;
                existingQuestion.Answer = question.Answer;
                existingQuestion.Comment = question.Comment;
            }
            else
            {
                DbContext.Questions.Add(new Question
                {
                    QuestionNumber = question.QuestionNumber,
                    Cost = question.Cost,
                    Text = question.Text,
                    Answer = question.Answer,
                    Comment = question.Comment,
                    TopicId = topicId,
                });
            }
        }
    }
}
