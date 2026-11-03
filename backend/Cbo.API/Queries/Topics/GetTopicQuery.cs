using Cbo.API.Contracts.Topics;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Topics;

public sealed record GetTopicQuery(Guid TopicId) : IQuery<TopicResponse>;
