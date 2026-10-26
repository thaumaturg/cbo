using Cbo.API.Commands.Topics;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;

namespace Cbo.API.CommandHandlers.Topics;

public sealed class CreateTopicCommandHandler(
    CboDbContext dbContext,
    ITopicValidationService topicValidationService,
    ICurrentUserService currentUserService) : CommandHandler<CreateTopicCommand, Guid>(dbContext)
{
    private readonly ITopicValidationService _topicValidationService = topicValidationService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    protected override Task<Result<Guid>> ExecuteAsync(CreateTopicCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(CreateTopic(command));

    private Result<Guid> CreateTopic(CreateTopicCommand command)
    {
        List<FieldError> fieldErrors = _topicValidationService.ValidateQuestions(
            command.Questions
                .Select(q => new QuestionValidationModel(q.QuestionNumber, q.CostPositive, q.CostNegative, q.Text, q.Answer))
                .ToList());
        if (fieldErrors.Count > 0)
            return TopicFailures.Invalid(fieldErrors);

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

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
                CostPositive = question.CostPositive,
                CostNegative = question.CostNegative,
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

        DbContext.Topics.Add(topic);

        return topic.Id;
    }
}
