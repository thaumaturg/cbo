using Cbo.Core.Domain;
using Cbo.Core.Validation;

namespace Cbo.Core.Participants;

public record CreateParticipantRequest
{
    public required string Username { get; set => field = RequestText.Trim(value); }
    public required TournamentParticipantRole Role { get; set; }
}
