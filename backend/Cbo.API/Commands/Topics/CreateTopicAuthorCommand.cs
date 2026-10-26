using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Topics;

public sealed record CreateTopicAuthorCommand(Guid TopicId, string Username) : ICommand<Guid>;
