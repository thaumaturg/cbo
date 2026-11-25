using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Paging;
using Cbo.Results;

namespace Cbo.Core.Tournaments;

public sealed class GetTournamentsQueryHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : IQueryHandler<GetTournamentsQuery, PageResponse<TournamentResponse>>
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<PageResponse<TournamentResponse>>> HandleAsync(GetTournamentsQuery query, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await TournamentListFilter.Apply(_dbContext.Tournaments, query, currentUserId)
            .ToPageAsync(
                TournamentListFilter.SortKeys,
                query.Sort,
                query.Page,
                query.PageSize,
                TournamentProjections.ToResponse(currentUserId),
                cancellationToken);
    }
}
