using Cbo.Core.Models.Constants;

namespace Cbo.Core.Contracts.Participants;

public record CreateParticipantRequest
{
    public required string Username { get; set; }
    public required TournamentParticipantRole Role { get; set; }
}
