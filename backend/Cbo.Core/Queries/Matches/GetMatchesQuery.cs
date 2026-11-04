using Cbo.Core.Contracts.Matches;
using Cbo.Core.Cqrs;

namespace Cbo.Core.Queries.Matches;

/// <summary>The tournament's matches, ordered by their number in the tournament.</summary>
public sealed record GetMatchesQuery(Guid TournamentId) : IQuery<List<MatchResponse>>;
