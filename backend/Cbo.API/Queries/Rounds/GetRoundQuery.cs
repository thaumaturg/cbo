using Cbo.API.Contracts.Rounds;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Rounds;

public sealed record GetRoundQuery(Guid TournamentId, Guid MatchId, int RoundNumber) : IQuery<RoundResponse>;
