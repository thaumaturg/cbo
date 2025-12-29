namespace Cbo.API.Models.DTO;

public record GetMatchParticipantDto
{
    public required int Id { get; set; }
    public required string Username { get; set; }
    public int? ScoreSum { get; set; }
    public decimal? PointsSum { get; set; }
}
