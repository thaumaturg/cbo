using Cbo.Api.Authorization;
using Cbo.Api.Contracts.Topics;
using Cbo.Api.Cqrs;
using Cbo.Api.Data;
using Cbo.Api.Mappings;
using Cbo.Api.Models.Domain;
using Cbo.Api.Queries.TournamentTopics;
using Cbo.Api.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Api.QueryHandlers.TournamentTopics;

public sealed class GetTournamentTopicQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    ICurrentUserService currentUserService) : IQueryHandler<GetTournamentTopicQuery, TopicResponse>
{
    private const string ViewAllTopicsForbiddenMessage = "You don't have permission to view all topics of this tournament.";
    private const string NotFoundMessage = "Topic is not part of this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<TopicResponse>> HandleAsync(GetTournamentTopicQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.ViewAllTopics, ViewAllTopicsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        TopicResponse? topic = await _dbContext.Topics
            .Where(t => t.Id == query.TopicId && t.TournamentTopics.Any(tt => tt.TournamentId == query.TournamentId))
            .Select(TopicProjections.ToResponse(currentUserId))
            .FirstOrDefaultAsync(cancellationToken);
        if (topic is null)
            return Failure.NotFound(NotFoundMessage);

        return topic;
    }
}
