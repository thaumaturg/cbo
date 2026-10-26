using Cbo.API.Authorization;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.TournamentTopics;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.QueryHandlers.TournamentTopics;

public sealed class GetMyTournamentTopicsQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    ICurrentUserService currentUserService) : IQueryHandler<GetMyTournamentTopicsQuery, List<GetTournamentTopicDto>>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<List<GetTournamentTopicDto>>> HandleAsync(GetMyTournamentTopicsQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.TournamentTopics
            .Where(tt => tt.TournamentId == query.TournamentId && tt.TournamentParticipant!.ApplicationUserId == currentUserId)
            .OrderBy(tt => tt.PriorityIndex)
            .Select(TournamentTopicProjections.ToGetDto())
            .ToListAsync(cancellationToken);
    }
}
