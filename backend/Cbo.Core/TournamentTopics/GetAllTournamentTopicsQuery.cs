using Cbo.Core.Cqrs;

namespace Cbo.Core.TournamentTopics;

/// <summary>Every participant's topics in the tournament, grouped by participant and ordered by priority.</summary>
public sealed record GetAllTournamentTopicsQuery(Guid TournamentId) : IQuery<List<TournamentTopicResponse>>;
