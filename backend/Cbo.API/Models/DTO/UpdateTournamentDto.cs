namespace Cbo.API.Models.DTO;

public class UpdateTournamentDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTime PlannedStart { get; set; }
}
