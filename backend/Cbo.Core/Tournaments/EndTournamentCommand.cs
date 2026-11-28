using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

public sealed record EndTournamentCommand(Guid TournamentId) : ICommand;
