using Cbo.Core.Cqrs;

namespace Cbo.Core.Commands.Tournaments;

public sealed record UpdateTournamentCommand(
    Guid TournamentId,
    string Title,
    string? Description,
    int? PlayersPerTournament,
    int? TopicsPerParticipantMax,
    int? TopicsPerParticipantMin) : ICommand;
