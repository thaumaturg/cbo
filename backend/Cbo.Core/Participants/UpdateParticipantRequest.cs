using Cbo.Core.Domain;

namespace Cbo.Core.Participants;

public record UpdateParticipantRequest
{
    public required TournamentParticipantRole Role { get; set; }
}
