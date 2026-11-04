using Cbo.Api.Contracts.Rounds;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Rounds;

public sealed record GetRoundQuery(Guid TournamentId, Guid MatchId, int RoundNumber) : IQuery<RoundResponse>;
