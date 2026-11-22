using Cbo.Core.Cqrs;

namespace Cbo.Core.Friends;

/// <summary>The current user's whole friend list, by username ignoring case. Not paged: the pickers need every friend.</summary>
public sealed record GetFriendsQuery : IQuery<List<FriendResponse>>;
