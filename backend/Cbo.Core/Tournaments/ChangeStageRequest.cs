using Cbo.Core.Domain;

namespace Cbo.Core.Tournaments;

public record ChangeStageRequest
{
    public required TournamentStage Stage { get; set; }
}
