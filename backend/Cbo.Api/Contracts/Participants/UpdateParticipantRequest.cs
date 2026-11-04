using Cbo.Api.Models.Constants;

namespace Cbo.Api.Contracts.Participants;

public record UpdateParticipantRequest
{
    public required TournamentParticipantRole Role { get; set; }
}
