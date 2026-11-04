using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.Topics;

public sealed record CreateTopicAuthorCommand(Guid TopicId, string Username) : ICommand<Guid>;
