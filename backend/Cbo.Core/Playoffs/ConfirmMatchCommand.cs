using Cbo.Core.Cqrs;

namespace Cbo.Core.Playoffs;

public sealed record ConfirmMatchCommand(Guid TournamentId, Guid MatchId) : ICommand;
