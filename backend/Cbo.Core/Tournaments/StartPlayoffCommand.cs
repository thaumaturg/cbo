using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

public sealed record StartPlayoffCommand(Guid TournamentId) : ICommand;
