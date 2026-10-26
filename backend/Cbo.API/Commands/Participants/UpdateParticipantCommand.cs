using Cbo.API.Cqrs;
using Cbo.API.Models.Constants;

namespace Cbo.API.Commands.Participants;

public sealed record UpdateParticipantCommand(Guid TournamentId, Guid ParticipantId, TournamentParticipantRole Role) : ICommand;
