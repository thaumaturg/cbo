using Cbo.Core.Cqrs;

namespace Cbo.Core.Matches;

/// <summary>
/// The tournament topics the match can still play: not played in the tournament yet, and not authored by any of the
/// match's players. Ordered by priority.
/// </summary>
public sealed record GetAvailableTopicsQuery(Guid TournamentId, Guid MatchId) : IQuery<List<AvailableTopicResponse>>;
