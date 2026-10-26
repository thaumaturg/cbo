using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.TournamentTopics;

/// <summary>A topic with its questions, if some participant chose it for the tournament.</summary>
public sealed record GetTournamentTopicQuery(Guid TournamentId, Guid TopicId) : IQuery<GetTopicDto>;
