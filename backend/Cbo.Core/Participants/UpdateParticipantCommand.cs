using Cbo.Core.Cqrs;
using Cbo.Core.Domain;

namespace Cbo.Core.Participants;

public sealed record UpdateParticipantCommand(Guid TournamentId, Guid ParticipantId, TournamentParticipantRole Role) : ICommand;
