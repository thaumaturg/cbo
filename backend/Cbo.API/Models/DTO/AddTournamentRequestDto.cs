namespace Cbo.API.Models.DTO;

public class AddTournamentRequestDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime PlannedStart { get; set; }
    public SettingsCreateDto Settings { get; set; }
}
