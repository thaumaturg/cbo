using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Topics;

public sealed record DeleteTopicCommand(Guid TopicId) : ICommand;
