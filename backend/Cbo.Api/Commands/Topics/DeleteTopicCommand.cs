using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.Topics;

public sealed record DeleteTopicCommand(Guid TopicId) : ICommand;
