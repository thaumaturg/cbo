using Cbo.Core.Contracts.Tournaments;
using Cbo.Core.Cqrs;

namespace Cbo.Core.Queries.Tournaments;

public sealed record GetTournamentQuery(Guid TournamentId) : IQuery<TournamentResponse>;
