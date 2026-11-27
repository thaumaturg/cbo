using Cbo.Core.Cqrs;
using Cbo.Core.Domain;

namespace Cbo.Core.Tournaments;

public sealed record CreateTournamentCommand(
    string Title,
    string? Description,
    TournamentFormat? Format,
    int? TopicsPerParticipantMax,
    int? TopicsPerParticipantMin) : ICommand<Guid>;
