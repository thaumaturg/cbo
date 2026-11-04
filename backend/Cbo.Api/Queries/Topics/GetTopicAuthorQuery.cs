using Cbo.Api.Contracts.Topics;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Topics;

public sealed record GetTopicAuthorQuery(Guid TopicId, Guid AuthorId) : IQuery<TopicAuthorResponse>;
