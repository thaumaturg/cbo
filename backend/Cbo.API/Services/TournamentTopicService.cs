using System.Security.Claims;
using Cbo.API.Authorization;
using Cbo.API.Mappings;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;

namespace Cbo.API.Services;

public interface ITournamentTopicService
{
    Task<Result<List<GetTournamentTopicDto>>> GetMyTopicsAsync(Guid tournamentId);
    Task<Result<List<GetTournamentTopicDto>>> GetAllTopicsAsync(Guid tournamentId);
    Task<Result<List<GetTournamentTopicDto>>> SetMyTopicsAsync(Guid tournamentId, List<UpdateTournamentTopicDto> topicsDto);
    Task<Result<GetTopicDto>> GetTopicAsync(Guid tournamentId, Guid topicId);
}

public class TournamentTopicService(
    ITournamentAccessService tournamentAccessService,
    ITournamentParticipantsRepository participantsRepository,
    ITournamentTopicRepository tournamentTopicRepository,
    ITopicRepository topicRepository,
    ICurrentUserService currentUserService,
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor) : ITournamentTopicService
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string ChooseTopicsForbiddenMessage = "You don't have permission to choose topics in this tournament.";
    private const string ViewAllTopicsForbiddenMessage = "You don't have permission to view all topics of this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ITournamentParticipantsRepository _participantsRepository = participantsRepository;
    private readonly ITournamentTopicRepository _tournamentTopicRepository = tournamentTopicRepository;
    private readonly ITopicRepository _topicRepository = topicRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<Result<List<GetTournamentTopicDto>>> GetMyTopicsAsync(Guid tournamentId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.Read, ReadForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        TournamentParticipant participant = await GetCurrentParticipantAsync(tournamentId);

        List<TournamentTopic> topics = await _tournamentTopicRepository.GetAllByParticipantIdAsync(tournamentId, participant.Id);

        return topics.Select(t => t.ToGetDto()).ToList();
    }

    public async Task<Result<List<GetTournamentTopicDto>>> GetAllTopicsAsync(Guid tournamentId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.ViewAllTopics, ViewAllTopicsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        List<TournamentTopic> topics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);

        return topics.Select(t => t.ToGetDto()).ToList();
    }

    public async Task<Result<List<GetTournamentTopicDto>>> SetMyTopicsAsync(Guid tournamentId, List<UpdateTournamentTopicDto> topicsDto)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.Read, ChooseTopicsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;
        TournamentParticipant participant = await GetCurrentParticipantAsync(tournamentId);

        // The count is wrong for the list as a whole, not for one item, so it has no field error
        if (participant.Role == TournamentParticipantRole.Player && topicsDto.Count < tournament.TopicsPerParticipantMin)
            return Failure.Invalid($"You must choose at least {tournament.TopicsPerParticipantMin} topics.");

        if (topicsDto.Count > tournament.TopicsPerParticipantMax)
            return Failure.Invalid($"You can choose at most {tournament.TopicsPerParticipantMax} topics.");

        List<FieldError> fieldErrors = await ValidateTopicIdsAsync(topicsDto);
        if (fieldErrors.Count > 0)
            return Failure.Invalid("Some of the chosen topics are invalid.", fieldErrors);

        if (tournament.CurrentStage != TournamentStage.Preparations)
            return Failure.Conflict("Topics can only be changed during the Preparations stage.");

        List<TournamentTopic> topics = topicsDto
            .Select(dto => dto.ToNewTournamentTopic(tournamentId, participant.Id))
            .ToList();

        List<TournamentTopic> savedTopics = await _tournamentTopicRepository.SetTopicsForParticipantAsync(tournamentId, participant.Id, topics);

        return savedTopics.Select(t => t.ToGetDto()).ToList();
    }

    public async Task<Result<GetTopicDto>> GetTopicAsync(Guid tournamentId, Guid topicId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.ViewAllTopics, ViewAllTopicsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        List<TournamentTopic> tournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);
        if (!tournamentTopics.Any(tt => tt.TopicId == topicId))
            return TopicNotFound();

        Topic? topic = await _topicRepository.GetByIdIncludeQuestionsAsync(topicId);
        if (topic is null)
            return TopicNotFound();

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();
        TopicAuthor? topicAuthor = topic.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUserId);
        bool isAuthor = topicAuthor?.IsAuthor ?? false;
        bool isPlayed = topic.Rounds.Count > 0;

        return topic.ToGetDto(isPlayed, isAuthor);
    }

    /// <summary>
    /// Every topic id in the request must exist, appear at most once, and be a topic the current user owns.
    /// </summary>
    private async Task<List<FieldError>> ValidateTopicIdsAsync(List<UpdateTournamentTopicDto> topicsDto)
    {
        List<FieldError> fieldErrors = [];
        HashSet<Guid> seenTopicIds = [];
        ClaimsPrincipal user = GetCurrentUser();

        for (int i = 0; i < topicsDto.Count; i++)
        {
            Guid topicId = topicsDto[i].TopicId;
            string field = $"[{i}].topicId";

            if (!seenTopicIds.Add(topicId))
            {
                fieldErrors.Add(new FieldError(field, "This topic appears more than once."));
                continue;
            }

            Topic? topic = await _topicRepository.GetByIdAsync(topicId);
            if (topic is null)
            {
                fieldErrors.Add(new FieldError(field, "This topic doesn't exist."));
                continue;
            }

            AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(user, topic, TopicOperations.Read);
            if (!authResult.Succeeded)
                fieldErrors.Add(new FieldError(field, "You can only choose topics you own."));
        }

        return fieldErrors;
    }

    /// <summary>
    /// Every user who passed a tournament permission check is a participant of it, so a missing participant is a bug.
    /// </summary>
    private async Task<TournamentParticipant> GetCurrentParticipantAsync(Guid tournamentId)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _participantsRepository.GetByUserIdAndTournamentIdAsync(currentUserId, tournamentId)
            ?? throw new InvalidOperationException($"User {currentUserId} passed a permission check on tournament {tournamentId} but isn't a participant of it.");
    }

    private ClaimsPrincipal GetCurrentUser()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("No authenticated user in the current request. Tournament topic operations must only be called from [Authorize] endpoints.");

        return user;
    }

    private static Failure TopicNotFound() =>
        Failure.NotFound("Topic is not part of this tournament.");
}
