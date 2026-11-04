using Cbo.Core.Models.Constants;

namespace Cbo.Core.Tournaments;

public record StartTournamentRequest
{
    public required TournamentStage Stage { get; set; }
}
