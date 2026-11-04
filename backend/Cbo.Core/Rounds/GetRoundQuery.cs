using Cbo.Core.Cqrs;

namespace Cbo.Core.Rounds;

public sealed record GetRoundQuery(Guid TournamentId, Guid MatchId, int RoundNumber) : IQuery<RoundResponse>;
