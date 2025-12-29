using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public record GetTournamentParticipantDto
{
    public required int Id { get; set; }
    public required string Username { get; set; }
    public required TournamentParticipantRole Role { get; set; }
    public int? ScoreSum { get; set; }
    public decimal? PointsSum { get; set; }
    public required int TournamentId { get; set; }
    public required int ApplicationUserId { get; set; }
}
