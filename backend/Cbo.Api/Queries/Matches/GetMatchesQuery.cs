using Cbo.Api.Contracts.Matches;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Matches;

/// <summary>The tournament's matches, ordered by their number in the tournament.</summary>
public sealed record GetMatchesQuery(Guid TournamentId) : IQuery<List<MatchResponse>>;
