namespace Cbo.API.Models.DTO;

public class TournamentDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string CurrentStage { get; set; }
    public DateTime PlannedStart { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
