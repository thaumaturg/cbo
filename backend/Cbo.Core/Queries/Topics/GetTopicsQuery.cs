using Cbo.Core.Contracts.Topics;
using Cbo.Core.Cqrs;

namespace Cbo.Core.Queries.Topics;

/// <summary>The topics the current user owns.</summary>
public sealed record GetTopicsQuery : IQuery<List<TopicResponse>>;
