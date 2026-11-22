using Cbo.Core.Cqrs;

namespace Cbo.Core.Friends;

public sealed record RemoveFriendCommand(Guid FriendId) : ICommand;
