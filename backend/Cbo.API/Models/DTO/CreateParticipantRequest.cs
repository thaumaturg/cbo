using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public record CreateParticipantRequest
{
    public required string Username { get; set; }
    public required TournamentParticipantRole Role { get; set; }
}
