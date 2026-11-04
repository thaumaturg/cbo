using Cbo.Api.Contracts.Topics;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.TournamentTopics;

/// <summary>A topic with its questions, if some participant chose it for the tournament.</summary>
public sealed record GetTournamentTopicQuery(Guid TournamentId, Guid TopicId) : IQuery<TopicResponse>;
