using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Tournaments;

public sealed record GetTournamentQuery(Guid TournamentId) : IQuery<GetTournamentDto>;
