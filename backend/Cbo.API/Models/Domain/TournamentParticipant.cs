using Cbo.API.Models.Constants;

namespace Cbo.API.Models.Domain;

public class TournamentParticipant
{
    public Guid Id { get; set; }
    public required TournamentParticipantRole Role { get; set; }
    public int? ScoreSum { get; set; }
    public decimal? PointsSum { get; set; }
    public Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
    public required Guid ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
    public ICollection<MatchParticipant> MatchParticipants { get; set; } = [];
    public ICollection<TournamentTopic> TournamentTopics { get; set; } = [];
}
