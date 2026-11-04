using Cbo.Core.Cqrs;

namespace Cbo.Core.Matches;

public sealed record GetMatchQuery(Guid TournamentId, Guid MatchId) : IQuery<MatchDetailResponse>;
