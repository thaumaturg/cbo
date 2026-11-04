using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

public sealed record CreateTopicAuthorCommand(Guid TopicId, string Username) : ICommand<Guid>;
