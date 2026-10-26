using Cbo.API.Authorization;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Matches;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.QueryHandlers.Matches;

public sealed class GetAvailableTopicsQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetAvailableTopicsQuery, List<GetAvailableTopicDto>>
{
    private const string ViewAllTopicsForbiddenMessage = "You don't have permission to view all topics of this tournament.";
    private const string NotFoundMessage = "Match not found in this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<List<GetAvailableTopicDto>>> HandleAsync(GetAvailableTopicsQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.ViewAllTopics, ViewAllTopicsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        bool matchExists = await _dbContext.Matches
            .AnyAsync(m => m.Id == query.MatchId && m.TournamentId == query.TournamentId, cancellationToken);
        if (!matchExists)
            return Failure.NotFound(NotFoundMessage);

        IQueryable<Guid> matchPlayerUserIds = _dbContext.MatchParticipants
            .Where(mp => mp.MatchId == query.MatchId)
            .Select(mp => mp.TournamentParticipant!.ApplicationUserId);

        return await _dbContext.TournamentTopics
            .Where(tt => tt.TournamentId == query.TournamentId)
            .Where(tt => !_dbContext.Rounds.Any(r => r.TopicId == tt.TopicId && r.Match!.TournamentId == query.TournamentId))
            .Where(tt => !tt.Topic!.TopicAuthors.Any(ta => ta.IsAuthor && matchPlayerUserIds.Contains(ta.ApplicationUserId)))
            .OrderBy(tt => tt.PriorityIndex)
            .ThenBy(tt => tt.Id)
            .Select(tt => new GetAvailableTopicDto
            {
                PriorityIndex = tt.PriorityIndex,
                TopicId = tt.TopicId,
                TopicTitle = tt.Topic!.Title,
                OwnerUsername = tt.TournamentParticipant!.ApplicationUser!.UserName ?? string.Empty,
            })
            .ToListAsync(cancellationToken);
    }
}
