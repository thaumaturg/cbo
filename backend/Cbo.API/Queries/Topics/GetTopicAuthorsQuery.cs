using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Topics;

/// <summary>The topic's authors, ordered by id.</summary>
public sealed record GetTopicAuthorsQuery(Guid TopicId) : IQuery<List<GetTopicAuthorDto>>;
