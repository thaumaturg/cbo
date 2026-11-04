using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

public sealed record GetTournamentQuery(Guid TournamentId) : IQuery<TournamentResponse>;
