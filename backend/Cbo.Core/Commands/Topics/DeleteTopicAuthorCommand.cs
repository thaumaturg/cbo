using Cbo.Core.Cqrs;

namespace Cbo.Core.Commands.Topics;

public sealed record DeleteTopicAuthorCommand(Guid TopicId, Guid AuthorId) : ICommand;
