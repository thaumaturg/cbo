using Cbo.API.Contracts.Topics;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Topics;

/// <summary>The topics the current user owns.</summary>
public sealed record GetTopicsQuery : IQuery<List<TopicResponse>>;
