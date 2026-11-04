using Cbo.Core.Cqrs;
using Cbo.Core.Models.Constants;

namespace Cbo.Core.Commands.Participants;

public sealed record CreateParticipantCommand(Guid TournamentId, string Username, TournamentParticipantRole Role) : ICommand<Guid>;
