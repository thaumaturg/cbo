using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

/// <summary>The topics the current user owns.</summary>
public sealed record GetTopicsQuery : IQuery<List<TopicResponse>>;
