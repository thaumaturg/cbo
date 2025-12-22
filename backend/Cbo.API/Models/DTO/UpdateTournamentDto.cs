namespace Cbo.API.Models.DTO;

public record UpdateTournamentDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int? PlayersPerTournament { get; set; }
    public int? TopicsPerParticipantMax { get; set; }
    public int? TopicsPerParticipantMin { get; set; }
}
