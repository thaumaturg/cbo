using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Tournaments;

public sealed record StartTournamentCommand(Guid TournamentId) : ICommand;
