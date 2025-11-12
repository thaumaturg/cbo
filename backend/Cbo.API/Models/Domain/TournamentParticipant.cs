using Cbo.API.Models.Constants;

namespace Cbo.API.Models.Domain;

public class TournamentParticipant
{
    public required int Id { get; set; }
    public required TournamentParticipantRole Role { get; set; }
    public required int PointsSum { get; set; }
    public required int TournamentId { get; set; }
    public required Tournament Tournament { get; set; }
    public required int ApplicationUserId { get; set; }
    public required ApplicationUser ApplicationUser { get; set; }
    public ICollection<MatchParticipant> MatchParticipants { get; set; } = [];
}
