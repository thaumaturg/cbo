using Cbo.Core.Cqrs;
using Cbo.Core.Models.Constants;

namespace Cbo.Core.Commands.Participants;

public sealed record UpdateParticipantCommand(Guid TournamentId, Guid ParticipantId, TournamentParticipantRole Role) : ICommand;
