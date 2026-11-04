using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

public sealed record GetTopicQuery(Guid TopicId) : IQuery<TopicResponse>;
