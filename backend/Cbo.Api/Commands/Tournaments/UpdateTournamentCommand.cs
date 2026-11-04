using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.Tournaments;

public sealed record UpdateTournamentCommand(
    Guid TournamentId,
    string Title,
    string? Description,
    int? PlayersPerTournament,
    int? TopicsPerParticipantMax,
    int? TopicsPerParticipantMin) : ICommand;
