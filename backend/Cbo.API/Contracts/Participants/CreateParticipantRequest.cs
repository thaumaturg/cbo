using Cbo.API.Models.Constants;

namespace Cbo.API.Contracts.Participants;

public record CreateParticipantRequest
{
    public required string Username { get; set; }
    public required TournamentParticipantRole Role { get; set; }
}
