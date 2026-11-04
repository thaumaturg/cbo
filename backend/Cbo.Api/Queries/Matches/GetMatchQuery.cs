using Cbo.Api.Contracts.Matches;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Matches;

public sealed record GetMatchQuery(Guid TournamentId, Guid MatchId) : IQuery<MatchDetailResponse>;
