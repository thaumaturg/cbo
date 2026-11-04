using Cbo.Core.Domain;

namespace Cbo.Core.Tournaments;

public record StartTournamentRequest
{
    public required TournamentStage Stage { get; set; }
}
