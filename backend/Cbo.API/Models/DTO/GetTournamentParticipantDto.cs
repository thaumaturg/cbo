using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public record GetTournamentParticipantDto
{
    public required Guid Id { get; set; }
    public required string Username { get; set; }
    public required TournamentParticipantRole Role { get; set; }
    public int? ScoreSum { get; set; }
    public decimal? PointsSum { get; set; }
    public required int TopicsCount { get; set; }
    public required Guid TournamentId { get; set; }
    public required Guid ApplicationUserId { get; set; }
}
