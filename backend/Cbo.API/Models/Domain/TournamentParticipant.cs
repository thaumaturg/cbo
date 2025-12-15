using Cbo.API.Models.Constants;

namespace Cbo.API.Models.Domain;

public class TournamentParticipant
{
    public int Id { get; set; }
    public required TournamentParticipantRole Role { get; set; }
    public int? PointsSum { get; set; }
    public int TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
    public required int ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
    public ICollection<MatchParticipant> MatchParticipants { get; set; } = [];
    public ICollection<TournamentTopic> TournamentTopics { get; set; } = [];
}
