using System.Linq.Expressions;
using Cbo.Core.Domain;
using Cbo.Core.Tournaments;

namespace Cbo.Core.Friends;

/// <inheritdoc cref="TournamentProjections"/>
public static class FriendProjections
{
    public static Expression<Func<Friendship, FriendResponse>> ToResponse() =>
        f => new FriendResponse
        {
            AccountId = f.FriendId,
            Username = f.Friend!.UserName ?? string.Empty,
            AddedAt = f.CreatedAt,
        };
}
