using Cbo.Api.Contracts.Topics;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Topics;

/// <summary>The topic's authors, ordered by id.</summary>
public sealed record GetTopicAuthorsQuery(Guid TopicId) : IQuery<List<TopicAuthorResponse>>;
