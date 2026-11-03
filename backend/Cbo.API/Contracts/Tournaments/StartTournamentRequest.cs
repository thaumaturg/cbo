using Cbo.API.Models.Constants;

namespace Cbo.API.Contracts.Tournaments;

public record StartTournamentRequest
{
    public required TournamentStage Stage { get; set; }
}
