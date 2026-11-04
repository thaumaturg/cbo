using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

public sealed record DeleteTopicCommand(Guid TopicId) : ICommand;
