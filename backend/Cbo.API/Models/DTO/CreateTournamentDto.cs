namespace Cbo.API.Models.DTO;

public class CreateTournamentDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTime PlannedStart { get; set; }
    public required CreateSettingsDto Settings { get; set; }
}
