using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

public sealed record DeleteTournamentCommand(Guid TournamentId) : ICommand;
