using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Friends;

public sealed class GetFriendsQueryHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : IQueryHandler<GetFriendsQuery, List<FriendResponse>>
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<List<FriendResponse>>> HandleAsync(GetFriendsQuery query, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.Friendships
            .Where(f => f.ApplicationUserId == currentUserId)
            .OrderBy(f => f.Friend!.NormalizedUserName)
            .ThenBy(f => f.FriendId)
            .Select(FriendProjections.ToResponse())
            .ToListAsync(cancellationToken);
    }
}
