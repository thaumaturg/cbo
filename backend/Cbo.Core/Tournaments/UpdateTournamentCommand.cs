using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

public sealed record UpdateTournamentCommand(
    Guid TournamentId,
    string Title,
    string? Description,
    int? PlayersPerTournament,
    int? TopicsPerParticipantMax,
    int? TopicsPerParticipantMin) : ICommand;
