using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

public sealed record GetTopicAuthorQuery(Guid TopicId, Guid AuthorId) : IQuery<TopicAuthorResponse>;
