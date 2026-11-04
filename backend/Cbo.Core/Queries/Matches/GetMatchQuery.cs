using Cbo.Core.Contracts.Matches;
using Cbo.Core.Cqrs;

namespace Cbo.Core.Queries.Matches;

public sealed record GetMatchQuery(Guid TournamentId, Guid MatchId) : IQuery<MatchDetailResponse>;
