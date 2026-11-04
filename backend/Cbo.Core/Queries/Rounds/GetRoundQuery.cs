using Cbo.Core.Contracts.Rounds;
using Cbo.Core.Cqrs;

namespace Cbo.Core.Queries.Rounds;

public sealed record GetRoundQuery(Guid TournamentId, Guid MatchId, int RoundNumber) : IQuery<RoundResponse>;
