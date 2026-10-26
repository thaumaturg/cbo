using System.Security.Claims;
using Cbo.API.Authorization;
using Cbo.API.Commands.TournamentTopics;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.TournamentTopics;

public sealed class SetMyTournamentTopicsCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    ICurrentUserService currentUserService,
    IAuthorizationService authorizationService) : CommandHandler<SetMyTournamentTopicsCommand>(dbContext)
{
    private const string ChooseTopicsForbiddenMessage = "You don't have permission to choose topics in this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    protected override async Task<Result> ExecuteAsync(SetMyTournamentTopicsCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.Read, ChooseTopicsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;
        TournamentParticipant participant = await GetCurrentParticipantAsync(command.TournamentId, cancellationToken);

        // The count is wrong for the list as a whole, not for one item, so it has no field error
        if (participant.Role == TournamentParticipantRole.Player && command.Topics.Count < tournament.TopicsPerParticipantMin)
            return Failure.Invalid($"You must choose at least {tournament.TopicsPerParticipantMin} topics.");

        if (command.Topics.Count > tournament.TopicsPerParticipantMax)
            return Failure.Invalid($"You can choose at most {tournament.TopicsPerParticipantMax} topics.");

        List<FieldError> fieldErrors = await ValidateTopicIdsAsync(command.Topics, cancellationToken);
        if (fieldErrors.Count > 0)
            return Failure.Invalid("Some of the chosen topics are invalid.", fieldErrors);

        if (tournament.CurrentStage != TournamentStage.Preparations)
            return Failure.Conflict("Topics can only be changed during the Preparations stage.");

        await DbContext.TournamentTopics
            .Where(tt => tt.TournamentId == command.TournamentId && tt.TournamentParticipantId == participant.Id)
            .ExecuteDeleteAsync(cancellationToken);

        DbContext.TournamentTopics.AddRange(command.Topics.Select(topic => new TournamentTopic
        {
            PriorityIndex = topic.PriorityIndex,
            TournamentId = command.TournamentId,
            TopicId = topic.TopicId,
            TournamentParticipantId = participant.Id,
        }));

        return Result.Success();
    }

    /// <summary>
    /// Every topic id in the request must exist, appear at most once, and be a topic the current user owns.
    /// </summary>
    private async Task<List<FieldError>> ValidateTopicIdsAsync(IReadOnlyList<ChosenTopic> topics, CancellationToken cancellationToken)
    {
        List<Guid> topicIds = topics.Select(t => t.TopicId).Distinct().ToList();
        Dictionary<Guid, Topic> topicsById = await DbContext.Topics
            .AsNoTracking()
            .Where(t => topicIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        List<FieldError> fieldErrors = [];
        HashSet<Guid> seenTopicIds = [];
        ClaimsPrincipal user = _currentUserService.GetRequiredCurrentUser();

        for (int i = 0; i < topics.Count; i++)
        {
            Guid topicId = topics[i].TopicId;
            string field = $"[{i}].topicId";

            if (!seenTopicIds.Add(topicId))
            {
                fieldErrors.Add(new FieldError(field, "This topic appears more than once."));
                continue;
            }

            if (!topicsById.TryGetValue(topicId, out Topic? topic))
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
    private async Task<TournamentParticipant> GetCurrentParticipantAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await DbContext.TournamentParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.ApplicationUserId == currentUserId && tp.TournamentId == tournamentId, cancellationToken)
            ?? throw new InvalidOperationException($"User {currentUserId} passed a permission check on tournament {tournamentId} but isn't a participant of it.");
    }
}
