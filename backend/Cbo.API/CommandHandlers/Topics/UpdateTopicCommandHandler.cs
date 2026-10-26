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
/// Updates the topic's details and syncs its questions: questions with an id are changed, questions without one are
/// added, and the topic's other questions are removed.
/// </summary>
public sealed class UpdateTopicCommandHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService,
    ITopicValidationService topicValidationService,
    ICurrentUserService currentUserService) : CommandHandler<UpdateTopicCommand>(dbContext)
{
    private const string UpdateForbiddenMessage = "You don't have permission to edit this topic.";

    private readonly ITopicAccessService _topicAccessService = topicAccessService;
    private readonly ITopicValidationService _topicValidationService = topicValidationService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    protected override async Task<Result> ExecuteAsync(UpdateTopicCommand command, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            command.TopicId, TopicOperations.Update, UpdateForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Topic topic = access.Value;

        List<Question> existingQuestions = await DbContext.Questions
            .Where(q => q.TopicId == command.TopicId)
            .ToListAsync(cancellationToken);

        List<FieldError> fieldErrors = _topicValidationService.ValidateQuestions(
            command.Questions
                .Select(q => new QuestionValidationModel(q.QuestionNumber, q.CostPositive, q.CostNegative, q.Text, q.Answer))
                .ToList());
        fieldErrors.AddRange(ValidateQuestionIds(existingQuestions, command.Questions));
        if (fieldErrors.Count > 0)
            return TopicFailures.Invalid(fieldErrors);

        if (await TopicRules.IsPlayedAsync(DbContext, command.TopicId, cancellationToken))
            return Failure.Conflict("This topic has been played in a match, so it can't be edited.");

        topic.Title = command.Title;
        topic.Description = command.Description;

        SyncQuestions(command.TopicId, existingQuestions, command.Questions);

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();
        TopicAuthor? currentAuthor = await DbContext.TopicAuthors
            .FirstOrDefaultAsync(ta => ta.TopicId == command.TopicId && ta.ApplicationUserId == currentUserId, cancellationToken);
        if (currentAuthor is not null)
            currentAuthor.IsAuthor = command.IsAuthor;

        return Result.Success();
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
                existingQuestion.CostPositive = question.CostPositive;
                existingQuestion.CostNegative = question.CostNegative;
                existingQuestion.Text = question.Text;
                existingQuestion.Answer = question.Answer;
                existingQuestion.Comment = question.Comment;
            }
            else
            {
                DbContext.Questions.Add(new Question
                {
                    QuestionNumber = question.QuestionNumber,
                    CostPositive = question.CostPositive,
                    CostNegative = question.CostNegative,
                    Text = question.Text,
                    Answer = question.Answer,
                    Comment = question.Comment,
                    TopicId = topicId,
                });
            }
        }
    }
}
