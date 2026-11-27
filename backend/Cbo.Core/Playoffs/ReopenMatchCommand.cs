using Cbo.Core.Cqrs;

namespace Cbo.Core.Playoffs;

public sealed record ReopenMatchCommand(Guid TournamentId, Guid MatchId) : ICommand;
