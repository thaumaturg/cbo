using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Friends;

/// <summary>
/// Takes an account off the current user's friend list. It stays on every topic and tournament it's already on.
/// </summary>
public sealed class RemoveFriendCommandHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : CommandHandler<RemoveFriendCommand>(dbContext)
{
    private readonly ICurrentUserService _currentUserService = currentUserService;

    protected override async Task<Result> ExecuteAsync(RemoveFriendCommand command, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        Friendship? friendship = await DbContext.Friendships
            .FirstOrDefaultAsync(f => f.ApplicationUserId == currentUserId && f.FriendId == command.FriendId, cancellationToken);
        if (friendship is null)
            return FriendFailures.NotFound();

        DbContext.Friendships.Remove(friendship);

        return Result.Success();
    }
}
