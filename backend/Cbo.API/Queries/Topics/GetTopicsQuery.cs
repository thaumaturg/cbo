using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Topics;

/// <summary>The topics the current user owns.</summary>
public sealed record GetTopicsQuery : IQuery<List<GetTopicDto>>;
