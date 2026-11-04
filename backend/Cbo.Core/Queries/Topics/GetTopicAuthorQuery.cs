using Cbo.Core.Contracts.Topics;
using Cbo.Core.Cqrs;

namespace Cbo.Core.Queries.Topics;

public sealed record GetTopicAuthorQuery(Guid TopicId, Guid AuthorId) : IQuery<TopicAuthorResponse>;
