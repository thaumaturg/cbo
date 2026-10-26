using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Topics;

public sealed record GetTopicQuery(Guid TopicId) : IQuery<GetTopicDto>;
