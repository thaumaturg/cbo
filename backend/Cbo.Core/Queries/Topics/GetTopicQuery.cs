using Cbo.Core.Contracts.Topics;
using Cbo.Core.Cqrs;

namespace Cbo.Core.Queries.Topics;

public sealed record GetTopicQuery(Guid TopicId) : IQuery<TopicResponse>;
