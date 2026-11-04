using Cbo.Api.Contracts.Topics;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Topics;

public sealed record GetTopicQuery(Guid TopicId) : IQuery<TopicResponse>;
