using Cbo.Core.Cqrs;

namespace Cbo.Core.Commands.Tournaments;

public sealed record DeleteTournamentCommand(Guid TournamentId) : ICommand;
