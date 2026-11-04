using Cbo.Core.Models.Constants;

namespace Cbo.Core.Participants;

public record UpdateParticipantRequest
{
    public required TournamentParticipantRole Role { get; set; }
}
