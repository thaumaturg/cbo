using System.Security.Claims;
using Cbo.API.Authorization;
using Cbo.API.Data.Configuration;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cbo.API.Services;

public interface ITopicService
{
    Task<List<GetTopicDto>> GetTopicsAsync();
    Task<Result<GetTopicDto>> GetTopicAsync(Guid topicId);
    Task<Result<GetTopicDto>> CreateTopicAsync(CreateTopicDto createTopicDto);
    Task<Result<GetTopicDto>> UpdateTopicAsync(Guid topicId, UpdateTopicDto updateTopicDto);
    Task<Result> DeleteTopicAsync(Guid topicId);

    Task<Result<List<GetTopicAuthorDto>>> GetAuthorsAsync(Guid topicId);
    Task<Result<GetTopicAuthorDto>> GetAuthorAsync(Guid topicId, Guid authorId);
    Task<Result<GetTopicAuthorDto>> CreateAuthorAsync(Guid topicId, CreateTopicAuthorDto createAuthorDto);
    Task<Result> DeleteAuthorAsync(Guid topicId, Guid authorId);
}

public class TopicService(
    ITopicRepository topicRepository,
    ITopicAuthorRepository topicAuthorRepository,
    ITopicValidationService topicValidationService,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService,
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor) : ITopicService
{
    private const string ReadForbiddenMessage = "You don't have permission to view this topic.";
    private const string UpdateForbiddenMessage = "You don't have permission to edit this topic.";
    private const string DeleteForbiddenMessage = "You don't have permission to delete this topic.";
    private const string ManageAuthorsForbiddenMessage = "You don't have permission to manage the authors of this topic.";

    private readonly ITopicRepository _topicRepository = topicRepository;
    private readonly ITopicAuthorRepository _topicAuthorRepository = topicAuthorRepository;
    private readonly ITopicValidationService _topicValidationService = topicValidationService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<List<GetTopicDto>> GetTopicsAsync()
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        List<Topic> topics = await _topicRepository.GetAllByUserIdAsync(currentUserId);

        return topics.Select(topic => ToGetDto(topic, currentUserId)).ToList();
    }

    public async Task<Result<GetTopicDto>> GetTopicAsync(Guid topicId)
    {
        Result<Topic> access = await LoadTopicWithAccessAsync(topicId, TopicOperations.Read, ReadForbiddenMessage, includeQuestions: true);
        if (access.IsFailure)
            return access.Failure;

        return ToGetDto(access.Value, _currentUserService.GetRequiredCurrentUserId());
    }

    public async Task<Result<GetTopicDto>> CreateTopicAsync(CreateTopicDto createTopicDto)
    {
        List<FieldError> fieldErrors = _topicValidationService.ValidateQuestions(
            createTopicDto.Questions
                .Select(q => new QuestionValidationModel(q.QuestionNumber, q.CostPositive, q.CostNegative, q.Text, q.Answer))
                .ToList());
        if (fieldErrors.Count > 0)
            return InvalidTopic(fieldErrors);

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        Topic topic = createTopicDto.ToDomain();
        foreach (CreateQuestionDto questionDto in createTopicDto.Questions)
        {
            topic.Questions.Add(questionDto.ToDomain(Guid.Empty));
        }

        topic.TopicAuthors.Add(new TopicAuthor
        {
            IsOwner = true,
            IsAuthor = createTopicDto.IsAuthor,
            ApplicationUserId = currentUserId,
            TopicId = Guid.Empty
        });

        topic = await _topicRepository.CreateAsync(topic);

        return await LoadTopicDtoAsync(topic.Id, currentUserId);
    }

    public async Task<Result<GetTopicDto>> UpdateTopicAsync(Guid topicId, UpdateTopicDto updateTopicDto)
    {
        Result<Topic> access = await LoadTopicWithAccessAsync(topicId, TopicOperations.Update, UpdateForbiddenMessage, includeQuestions: true);
        if (access.IsFailure)
            return access.Failure;

        Topic existingTopic = access.Value;

        List<UpdateQuestionDto> questions = updateTopicDto.Questions.ToList();
        List<FieldError> fieldErrors = _topicValidationService.ValidateQuestions(
            questions
                .Select(q => new QuestionValidationModel(q.QuestionNumber, q.CostPositive, q.CostNegative, q.Text, q.Answer))
                .ToList());
        fieldErrors.AddRange(ValidateQuestionIds(existingTopic, questions));
        if (fieldErrors.Count > 0)
            return InvalidTopic(fieldErrors);

        if (IsPlayed(existingTopic))
            return Failure.Conflict("This topic has been played in a match, so it can't be edited.");

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        var updateParameters = new UpdateTopicParameters
        {
            Title = updateTopicDto.Title,
            Description = updateTopicDto.Description,
            IsAuthor = updateTopicDto.IsAuthor,
            Questions = questions.Select(q => new UpdateQuestionParameters
            {
                Id = q.Id,
                QuestionNumber = q.QuestionNumber,
                CostPositive = q.CostPositive,
                CostNegative = q.CostNegative,
                Text = q.Text,
                Answer = q.Answer,
                Comment = q.Comment
            }).ToList()
        };

        Topic? updatedTopic = await _topicRepository.UpdateAsync(topicId, updateParameters, currentUserId);
        if (updatedTopic is null)
            return TopicNotFound();

        return await LoadTopicDtoAsync(topicId, currentUserId);
    }

    public async Task<Result> DeleteTopicAsync(Guid topicId)
    {
        Result<Topic> access = await LoadTopicWithAccessAsync(topicId, TopicOperations.Delete, DeleteForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        if (IsPlayed(access.Value))
            return TopicPlayedCannotBeDeleted();

        Topic? deletedTopic;
        try
        {
            deletedTopic = await _topicRepository.DeleteAsync(topicId);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // Lost a race with a round that started using the topic after the check above
            return TopicPlayedCannotBeDeleted();
        }

        if (deletedTopic is null)
            return TopicNotFound();

        return Result.Success();
    }

    public async Task<Result<List<GetTopicAuthorDto>>> GetAuthorsAsync(Guid topicId)
    {
        Result<Topic> access = await LoadTopicWithAccessAsync(topicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        List<TopicAuthor> authors = await _topicAuthorRepository.GetAllByTopicIdAsync(topicId);

        return authors.Select(a => a.ToGetDto()).ToList();
    }

    public async Task<Result<GetTopicAuthorDto>> GetAuthorAsync(Guid topicId, Guid authorId)
    {
        Result<Topic> access = await LoadTopicWithAccessAsync(topicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        TopicAuthor? author = await _topicAuthorRepository.GetByAuthorIdAndTopicIdAsync(authorId, topicId);
        if (author is null)
            return AuthorNotFound();

        return author.ToGetDto();
    }

    public async Task<Result<GetTopicAuthorDto>> CreateAuthorAsync(Guid topicId, CreateTopicAuthorDto createAuthorDto)
    {
        Result<Topic> access = await LoadTopicWithAccessAsync(topicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        ApplicationUser? user = await _userManager.FindByNameAsync(createAuthorDto.Username);
        if (user is null)
            return Failure.Invalid(
                "Some of the author data is invalid.",
                new FieldError("username", $"User '{createAuthorDto.Username}' doesn't exist."));

        if (IsPlayed(access.Value))
            return TopicPlayedAuthorsCannotChange();

        TopicAuthor? existingAuthor = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(user.Id, topicId);
        if (existingAuthor is not null)
            return AlreadyAnAuthor(createAuthorDto.Username);

        var author = new TopicAuthor
        {
            IsOwner = false,
            IsAuthor = true,
            TopicId = topicId,
            ApplicationUserId = user.Id
        };

        try
        {
            author = await _topicAuthorRepository.CreateAsync(author);
        }
        catch (DbUpdateException exception) when (IsTopicAuthorUniqueViolation(exception))
        {
            // Lost a race with a simultaneous add of the same author
            return AlreadyAnAuthor(createAuthorDto.Username);
        }

        return author.ToGetDto();
    }

    public async Task<Result> DeleteAuthorAsync(Guid topicId, Guid authorId)
    {
        Result<Topic> access = await LoadTopicWithAccessAsync(topicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        TopicAuthor? existingAuthor = await _topicAuthorRepository.GetByAuthorIdAndTopicIdAsync(authorId, topicId);
        if (existingAuthor is null)
            return AuthorNotFound();

        if (existingAuthor.IsOwner)
            return Failure.Invalid("The topic's owner can't be removed from its authors.");

        if (IsPlayed(access.Value))
            return TopicPlayedAuthorsCannotChange();

        TopicAuthor? deletedAuthor = await _topicAuthorRepository.DeleteAsync(authorId);
        if (deletedAuthor is null)
            return AuthorNotFound();

        return Result.Success();
    }

    /// <summary>
    /// Loads the topic from the URL (with its rounds, and optionally its questions and authors) and checks the current user's permission on it.
    /// </summary>
    private async Task<Result<Topic>> LoadTopicWithAccessAsync(
        Guid topicId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage,
        bool includeQuestions = false)
    {
        Topic? topic = includeQuestions
            ? await _topicRepository.GetByIdIncludeQuestionsAsync(topicId)
            : await _topicRepository.GetByIdAsync(topicId);
        if (topic is null)
            return TopicNotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), topic, operation);
        if (!authResult.Succeeded)
            return Failure.Forbidden(forbiddenMessage);

        return topic;
    }

    /// <summary>
    /// Every question id in the request must be a question of this topic, and appear at most once.
    /// </summary>
    private static IEnumerable<FieldError> ValidateQuestionIds(Topic topic, List<UpdateQuestionDto> questions)
    {
        HashSet<Guid> topicQuestionIds = topic.Questions.Select(q => q.Id).ToHashSet();
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

    private ClaimsPrincipal GetCurrentUser()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("No authenticated user in the current request. Topic operations must only be called from [Authorize] endpoints.");

        return user;
    }

    /// <summary>
    /// Reloads a topic that is known to exist and maps it. A missing topic is a bug, so it throws.
    /// </summary>
    private async Task<GetTopicDto> LoadTopicDtoAsync(Guid topicId, Guid currentUserId)
    {
        Topic topic = await _topicRepository.GetByIdIncludeQuestionsAsync(topicId)
            ?? throw new InvalidOperationException($"Topic {topicId} could not be reloaded.");

        return ToGetDto(topic, currentUserId);
    }

    /// <summary>
    /// Requires: Questions, TopicAuthors (with ApplicationUser) and Rounds to be loaded.
    /// </summary>
    private static GetTopicDto ToGetDto(Topic topic, Guid currentUserId)
    {
        TopicAuthor? currentUserAuthor = topic.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUserId);
        bool isAuthor = currentUserAuthor?.IsAuthor ?? false;

        return topic.ToGetDto(IsPlayed(topic), isAuthor);
    }

    private static bool IsPlayed(Topic topic) => topic.Rounds.Count > 0;

    private static bool IsTopicAuthorUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: TopicAuthorConfiguration.TopicIdApplicationUserIdIndexName,
        };

    private static Failure InvalidTopic(IEnumerable<FieldError> fieldErrors) =>
        Failure.Invalid("Some of the topic data is invalid.", fieldErrors);

    private static Failure TopicNotFound() =>
        Failure.NotFound("Topic not found.");

    private static Failure AuthorNotFound() =>
        Failure.NotFound("Author not found for this topic.");

    private static Failure TopicPlayedCannotBeDeleted() =>
        Failure.Conflict("This topic has been played in a match, so it can't be deleted.");

    private static Failure TopicPlayedAuthorsCannotChange() =>
        Failure.Conflict("This topic has been played in a match, so its authors can't be changed.");

    private static Failure AlreadyAnAuthor(string username) =>
        Failure.Conflict($"User '{username}' is already an author of this topic.");
}
