using Cbo.API.Models.Constants;

namespace Cbo.API.Contracts.Participants;

public record UpdateParticipantRequest
{
    public required TournamentParticipantRole Role { get; set; }
}
