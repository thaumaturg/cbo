using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Matches;

public sealed record GetMatchQuery(Guid TournamentId, Guid MatchId) : IQuery<GetMatchDetailDto>;
