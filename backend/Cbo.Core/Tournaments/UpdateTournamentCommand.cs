using Cbo.Core.Cqrs;
using Cbo.Core.Domain;

namespace Cbo.Core.Tournaments;

public sealed record UpdateTournamentCommand(
    Guid TournamentId,
    string Title,
    string? Description,
    TournamentFormat? Format,
    int? TopicsPerParticipantMax,
    int? TopicsPerParticipantMin) : ICommand;
