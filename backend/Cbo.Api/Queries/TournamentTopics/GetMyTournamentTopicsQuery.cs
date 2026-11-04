using Cbo.Api.Contracts.TournamentTopics;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.TournamentTopics;

/// <summary>The current participant's topics in the tournament, ordered by priority.</summary>
public sealed record GetMyTournamentTopicsQuery(Guid TournamentId) : IQuery<List<TournamentTopicResponse>>;
