using Cbo.Api.Cqrs;
using Cbo.Api.Models.Constants;

namespace Cbo.Api.Commands.Participants;

public sealed record CreateParticipantCommand(Guid TournamentId, string Username, TournamentParticipantRole Role) : ICommand<Guid>;
