using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public record StartTournamentRequest
{
    public required TournamentStage Stage { get; set; }
}
