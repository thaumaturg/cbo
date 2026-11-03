using Cbo.API.Contracts.TournamentTopics;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.TournamentTopics;

/// <summary>The current participant's topics in the tournament, ordered by priority.</summary>
public sealed record GetMyTournamentTopicsQuery(Guid TournamentId) : IQuery<List<TournamentTopicResponse>>;
