using Cbo.API.Domain.Constants;

namespace Cbo.API.Domain.Models;

public class TournamentParticipant
{
    public int Id { get; set; }
    public TournamentParticipantRole Role { get; set; }
    public int PointsSum { get; set; }
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }

    public ICollection<MatchParticipant> MatchParticipants { get; set; }
} 
