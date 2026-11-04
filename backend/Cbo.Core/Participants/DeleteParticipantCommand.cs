using Cbo.Core.Cqrs;

namespace Cbo.Core.Participants;

public sealed record DeleteParticipantCommand(Guid TournamentId, Guid ParticipantId) : ICommand;
