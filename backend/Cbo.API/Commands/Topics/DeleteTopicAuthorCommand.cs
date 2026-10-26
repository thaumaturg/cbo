using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Topics;

public sealed record DeleteTopicAuthorCommand(Guid TopicId, Guid AuthorId) : ICommand;
