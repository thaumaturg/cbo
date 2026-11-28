using Cbo.Core.Cqrs;

namespace Cbo.Core.Playoffs;

public sealed record GetReopenDeletionsQuery(Guid TournamentId, Guid MatchId) : IQuery<List<ReopenDeletionResponse>>;
