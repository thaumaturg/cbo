using Cbo.Core.Cqrs;
using Cbo.Core.Models.Constants;

namespace Cbo.Core.Participants;

public sealed record CreateParticipantCommand(Guid TournamentId, string Username, TournamentParticipantRole Role) : ICommand<Guid>;
