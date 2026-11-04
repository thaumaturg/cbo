using Cbo.Api.Contracts.TournamentTopics;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.TournamentTopics;

/// <summary>Every participant's topics in the tournament, grouped by participant and ordered by priority.</summary>
public sealed record GetAllTournamentTopicsQuery(Guid TournamentId) : IQuery<List<TournamentTopicResponse>>;
