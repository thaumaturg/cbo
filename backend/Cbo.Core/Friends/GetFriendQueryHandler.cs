using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Friends;

public sealed class GetFriendQueryHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : IQueryHandler<GetFriendQuery, FriendResponse>
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<FriendResponse>> HandleAsync(GetFriendQuery query, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        FriendResponse? friend = await _dbContext.Friendships
            .Where(f => f.ApplicationUserId == currentUserId && f.FriendId == query.FriendId)
            .Select(FriendProjections.ToResponse())
            .FirstOrDefaultAsync(cancellationToken);
        if (friend is null)
            return FriendFailures.NotFound();

        return friend;
    }
}
