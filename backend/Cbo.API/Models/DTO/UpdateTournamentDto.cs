namespace Cbo.API.Models.DTO;

public class UpdateTournamentDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime PlannedStart { get; set; }
}
