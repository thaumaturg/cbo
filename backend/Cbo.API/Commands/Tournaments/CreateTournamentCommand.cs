using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Tournaments;

public sealed record CreateTournamentCommand(
    string Title,
    string? Description,
    int? PlayersPerTournament,
    int? TopicsPerParticipantMax,
    int? TopicsPerParticipantMin) : ICommand<Guid>;
