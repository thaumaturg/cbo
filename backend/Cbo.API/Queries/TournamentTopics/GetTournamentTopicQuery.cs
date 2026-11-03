using Cbo.API.Contracts.Topics;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.TournamentTopics;

/// <summary>A topic with its questions, if some participant chose it for the tournament.</summary>
public sealed record GetTournamentTopicQuery(Guid TournamentId, Guid TopicId) : IQuery<TopicResponse>;
