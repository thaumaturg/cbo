using Cbo.Api.Contracts.Tournaments;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Tournaments;

public sealed record GetTournamentQuery(Guid TournamentId) : IQuery<TournamentResponse>;
