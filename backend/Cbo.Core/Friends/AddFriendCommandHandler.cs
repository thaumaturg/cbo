using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Data.Configuration;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Friends;

public sealed class AddFriendCommandHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService,
    UserManager<ApplicationUser> userManager) : CommandHandler<AddFriendCommand, Guid>(dbContext)
{
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    protected override async Task<Result<Guid>> ExecuteAsync(AddFriendCommand command, CancellationToken cancellationToken)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        ApplicationUser? friend = await _userManager.FindByNameAsync(command.Username);

        bool isAlreadyAFriend = friend is not null && await DbContext.Friendships
            .AnyAsync(f => f.ApplicationUserId == currentUserId && f.FriendId == friend.Id, cancellationToken);

        Failure? failure = FriendRules.CheckNewFriend(command.Username, friend?.Id, currentUserId, isAlreadyAFriend);
        if (failure is not null)
            return failure;

        DbContext.Friendships.Add(new Friendship
        {
            ApplicationUserId = currentUserId,
            FriendId = friend!.Id,
        });

        return friend.Id;
    }

    protected override Failure? MapSaveFailure(AddFriendCommand command, DbUpdateException exception)
    {
        // Lost a race with a simultaneous add of the same friend
        if (PostgresConstraint.IsUniqueViolation(exception, FriendshipConfiguration.ApplicationUserIdFriendIdIndexName))
            return FriendFailures.AlreadyAFriend(command.Username);

        return null;
    }
}
