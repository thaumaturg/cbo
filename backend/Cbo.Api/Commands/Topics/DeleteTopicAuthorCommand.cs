using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.Topics;

public sealed record DeleteTopicAuthorCommand(Guid TopicId, Guid AuthorId) : ICommand;
