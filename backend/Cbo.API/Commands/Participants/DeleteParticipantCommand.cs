using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Participants;

public sealed record DeleteParticipantCommand(Guid TournamentId, Guid ParticipantId) : ICommand;
