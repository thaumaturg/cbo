using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.Rounds;

public sealed record DeleteRoundCommand(Guid TournamentId, Guid MatchId, int RoundNumber) : ICommand;
