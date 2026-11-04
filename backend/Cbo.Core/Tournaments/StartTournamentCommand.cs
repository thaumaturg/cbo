using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

public sealed record StartTournamentCommand(Guid TournamentId) : ICommand;
