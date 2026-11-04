using Cbo.Core.Models.Constants;

namespace Cbo.Core.Contracts.Participants;

public record UpdateParticipantRequest
{
    public required TournamentParticipantRole Role { get; set; }
}
