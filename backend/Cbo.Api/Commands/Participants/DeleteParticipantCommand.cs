using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.Participants;

public sealed record DeleteParticipantCommand(Guid TournamentId, Guid ParticipantId) : ICommand;
