using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Rounds;

public sealed record DeleteRoundCommand(Guid TournamentId, Guid MatchId, int RoundNumber) : ICommand;
