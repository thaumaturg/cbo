using Cbo.Api.Contracts.Topics;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Topics;

/// <summary>The topics the current user owns.</summary>
public sealed record GetTopicsQuery : IQuery<List<TopicResponse>>;
