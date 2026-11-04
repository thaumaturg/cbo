using Cbo.Core.Cqrs;
using Cbo.Core.Domain;

namespace Cbo.Core.Participants;

public sealed record CreateParticipantCommand(Guid TournamentId, string Username, TournamentParticipantRole Role) : ICommand<Guid>;
