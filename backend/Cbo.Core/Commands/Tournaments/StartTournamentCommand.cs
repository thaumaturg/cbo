using Cbo.Core.Cqrs;

namespace Cbo.Core.Commands.Tournaments;

public sealed record StartTournamentCommand(Guid TournamentId) : ICommand;
