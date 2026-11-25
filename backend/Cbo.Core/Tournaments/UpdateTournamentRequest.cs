using Cbo.Core.Validation;

namespace Cbo.Core.Tournaments;

public record UpdateTournamentRequest
{
    public required string Title { get; set => field = RequestText.Trim(value); }
    public string? Description { get; set => field = RequestText.TrimToNull(value); }
    public int? PlayersPerTournament { get; set; }
    public int? TopicsPerParticipantMax { get; set; }
    public int? TopicsPerParticipantMin { get; set; }
}
