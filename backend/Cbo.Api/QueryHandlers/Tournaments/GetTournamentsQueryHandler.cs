using Cbo.Api.Contracts.Tournaments;
using Cbo.Api.Cqrs;
using Cbo.Api.Data;
using Cbo.Api.Mappings;
using Cbo.Api.Queries.Tournaments;
using Cbo.Api.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Api.QueryHandlers.Tournaments;

public sealed class GetTournamentsQueryHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : IQueryHandler<GetTournamentsQuery, List<TournamentResponse>>
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<List<TournamentResponse>>> HandleAsync(GetTournamentsQuery query, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.Tournaments
            .Where(t => t.TournamentParticipants.Any(tp => tp.ApplicationUserId == currentUserId))
            .Select(TournamentProjections.ToResponse(currentUserId))
            .ToListAsync(cancellationToken);
    }
}
