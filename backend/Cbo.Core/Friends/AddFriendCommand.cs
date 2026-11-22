using Cbo.Core.Cqrs;

namespace Cbo.Core.Friends;

/// <summary>Adds the account with <paramref name="Username"/> to the current user's friend list. Returns its account id.</summary>
public sealed record AddFriendCommand(string Username) : ICommand<Guid>;
