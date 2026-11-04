using Cbo.Core.Cqrs;

namespace Cbo.Core.Commands.Topics;

public sealed record DeleteTopicCommand(Guid TopicId) : ICommand;
