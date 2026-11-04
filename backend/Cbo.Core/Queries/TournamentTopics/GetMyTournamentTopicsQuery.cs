using Cbo.Core.Contracts.TournamentTopics;
using Cbo.Core.Cqrs;

namespace Cbo.Core.Queries.TournamentTopics;

/// <summary>The current participant's topics in the tournament, ordered by priority.</summary>
public sealed record GetMyTournamentTopicsQuery(Guid TournamentId) : IQuery<List<TournamentTopicResponse>>;
