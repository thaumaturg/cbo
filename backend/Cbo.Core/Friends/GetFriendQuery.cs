using Cbo.Core.Cqrs;

namespace Cbo.Core.Friends;

/// <summary>One friend on the current user's friend list, by the friend's account id.</summary>
public sealed record GetFriendQuery(Guid FriendId) : IQuery<FriendResponse>;
