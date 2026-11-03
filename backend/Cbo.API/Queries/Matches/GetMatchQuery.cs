using Cbo.API.Contracts.Matches;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Matches;

public sealed record GetMatchQuery(Guid TournamentId, Guid MatchId) : IQuery<MatchDetailResponse>;
