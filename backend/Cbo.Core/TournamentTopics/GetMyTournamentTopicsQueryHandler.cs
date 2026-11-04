using Cbo.Core.Auth;
using Cbo.Core.Authorization;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Models.Domain;
using Cbo.Core.Tournaments;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.TournamentTopics;

public sealed class GetMyTournamentTopicsQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    ICurrentUserService currentUserService) : IQueryHandler<GetMyTournamentTopicsQuery, List<TournamentTopicResponse>>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<List<TournamentTopicResponse>>> HandleAsync(GetMyTournamentTopicsQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.TournamentTopics
            .Where(tt => tt.TournamentId == query.TournamentId && tt.TournamentParticipant!.ApplicationUserId == currentUserId)
            .OrderBy(tt => tt.PriorityIndex)
            .Select(TournamentTopicProjections.ToResponse())
            .ToListAsync(cancellationToken);
    }
}
