using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.Tournaments;

public sealed record CreateTournamentCommand(
    string Title,
    string? Description,
    int? PlayersPerTournament,
    int? TopicsPerParticipantMax,
    int? TopicsPerParticipantMin) : ICommand<Guid>;
