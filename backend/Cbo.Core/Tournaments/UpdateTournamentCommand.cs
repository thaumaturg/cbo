using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

public sealed record UpdateTournamentCommand(
    Guid TournamentId,
    string Title,
    string? Description,
    int? TournamentSize,
    int? TopicsPerParticipantMax,
    int? TopicsPerParticipantMin) : ICommand;
