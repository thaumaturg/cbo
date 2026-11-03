using Cbo.API.Contracts.Matches;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Matches;

/// <summary>
/// The tournament topics the match can still play: not played in the tournament yet, and not authored by any of the
/// match's players. Ordered by priority.
/// </summary>
public sealed record GetAvailableTopicsQuery(Guid TournamentId, Guid MatchId) : IQuery<List<AvailableTopicResponse>>;
