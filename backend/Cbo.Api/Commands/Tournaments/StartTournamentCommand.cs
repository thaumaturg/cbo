using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.Tournaments;

public sealed record StartTournamentCommand(Guid TournamentId) : ICommand;
