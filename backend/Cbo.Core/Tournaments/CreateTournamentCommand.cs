using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

public sealed record CreateTournamentCommand(
    string Title,
    string? Description,
    int? TournamentSize,
    int? TopicsPerParticipantMax,
    int? TopicsPerParticipantMin) : ICommand<Guid>;
