using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Tournaments;

public sealed record DeleteTournamentCommand(Guid TournamentId) : ICommand;
