using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public record UpdateTournamentParticipantDto
{
    public required TournamentParticipantRole Role { get; set; }
}
