using Cbo.Api.Models.Constants;

namespace Cbo.Api.Contracts.Participants;

public record CreateParticipantRequest
{
    public required string Username { get; set; }
    public required TournamentParticipantRole Role { get; set; }
}
