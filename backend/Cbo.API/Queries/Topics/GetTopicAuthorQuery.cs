using Cbo.API.Contracts.Topics;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Topics;

public sealed record GetTopicAuthorQuery(Guid TopicId, Guid AuthorId) : IQuery<TopicAuthorResponse>;
