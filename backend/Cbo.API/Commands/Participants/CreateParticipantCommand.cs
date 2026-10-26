using Cbo.API.Cqrs;
using Cbo.API.Models.Constants;

namespace Cbo.API.Commands.Participants;

public sealed record CreateParticipantCommand(Guid TournamentId, string Username, TournamentParticipantRole Role) : ICommand<Guid>;
