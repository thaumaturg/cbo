using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public record UpdateParticipantRequest
{
    public required TournamentParticipantRole Role { get; set; }
}
