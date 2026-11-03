using Cbo.API.Contracts.Tournaments;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Tournaments;

public sealed record GetTournamentQuery(Guid TournamentId) : IQuery<TournamentResponse>;
