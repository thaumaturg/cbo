using Cbo.Api.Models.Constants;

namespace Cbo.Api.Contracts.Tournaments;

public record StartTournamentRequest
{
    public required TournamentStage Stage { get; set; }
}
