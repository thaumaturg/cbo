namespace Cbo.API.Models.DTO;

public class GetTournamentDto
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string CurrentStage { get; set; }
    public required DateTime PlannedStart { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public GetSettingsDto? Settings { get; set; }
}
