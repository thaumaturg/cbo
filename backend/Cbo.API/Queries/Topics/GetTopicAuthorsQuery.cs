using Cbo.API.Contracts.Topics;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Topics;

/// <summary>The topic's authors, ordered by id.</summary>
public sealed record GetTopicAuthorsQuery(Guid TopicId) : IQuery<List<TopicAuthorResponse>>;
