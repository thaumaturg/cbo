using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Matches;

/// <summary>
/// The tournament topics the match can still play: not played in the tournament yet, and not authored by any of the
/// match's players. Ordered by priority.
/// </summary>
public sealed record GetAvailableTopicsQuery(Guid TournamentId, Guid MatchId) : IQuery<List<GetAvailableTopicDto>>;
