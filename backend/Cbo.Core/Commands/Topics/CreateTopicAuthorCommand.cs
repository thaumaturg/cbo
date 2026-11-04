using Cbo.Core.Cqrs;

namespace Cbo.Core.Commands.Topics;

public sealed record CreateTopicAuthorCommand(Guid TopicId, string Username) : ICommand<Guid>;
