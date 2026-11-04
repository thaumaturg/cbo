using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

public sealed record DeleteTopicAuthorCommand(Guid TopicId, Guid AuthorId) : ICommand;
