using Cbo.Core.Cqrs;

namespace Cbo.Core.Commands.Rounds;

public sealed record DeleteRoundCommand(Guid TournamentId, Guid MatchId, int RoundNumber) : ICommand;
