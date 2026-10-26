using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Topics;

public sealed record GetTopicAuthorQuery(Guid TopicId, Guid AuthorId) : IQuery<GetTopicAuthorDto>;
