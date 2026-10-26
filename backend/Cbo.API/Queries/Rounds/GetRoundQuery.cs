using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Rounds;

public sealed record GetRoundQuery(Guid TournamentId, Guid MatchId, int RoundNumber) : IQuery<GetRoundDto>;
