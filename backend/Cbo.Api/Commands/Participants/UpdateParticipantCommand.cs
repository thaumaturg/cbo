using Cbo.Api.Cqrs;
using Cbo.Api.Models.Constants;

namespace Cbo.Api.Commands.Participants;

public sealed record UpdateParticipantCommand(Guid TournamentId, Guid ParticipantId, TournamentParticipantRole Role) : ICommand;
