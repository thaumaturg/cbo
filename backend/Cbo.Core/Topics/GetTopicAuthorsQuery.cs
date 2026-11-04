using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

/// <summary>The topic's authors, ordered by id.</summary>
public sealed record GetTopicAuthorsQuery(Guid TopicId) : IQuery<List<TopicAuthorResponse>>;
