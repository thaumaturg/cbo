using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Tournaments;

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
