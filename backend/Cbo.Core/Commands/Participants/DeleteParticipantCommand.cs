using Cbo.Core.Cqrs;

namespace Cbo.Core.Commands.Participants;

public sealed record DeleteParticipantCommand(Guid TournamentId, Guid ParticipantId) : ICommand;
