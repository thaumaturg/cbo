using Cbo.API.Contracts.Matches;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Matches;

/// <summary>The tournament's matches, ordered by their number in the tournament.</summary>
public sealed record GetMatchesQuery(Guid TournamentId) : IQuery<List<MatchResponse>>;
