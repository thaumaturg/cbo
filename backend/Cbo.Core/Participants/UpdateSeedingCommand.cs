using Cbo.Core.Cqrs;

namespace Cbo.Core.Participants;

/// <summary>Player ids in the new seeding order. The first id becomes seed 1.</summary>
public sealed record UpdateSeedingCommand(Guid TournamentId, IReadOnlyList<Guid> OrderedParticipantIds) : ICommand;
