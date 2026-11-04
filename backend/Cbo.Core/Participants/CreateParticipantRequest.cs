using Cbo.Core.Models.Constants;

namespace Cbo.Core.Participants;

public record CreateParticipantRequest
{
    public required string Username { get; set; }
    public required TournamentParticipantRole Role { get; set; }
}
