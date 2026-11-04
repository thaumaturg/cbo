using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.Tournaments;

public sealed record DeleteTournamentCommand(Guid TournamentId) : ICommand;
