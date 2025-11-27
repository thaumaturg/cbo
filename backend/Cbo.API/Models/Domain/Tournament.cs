using Cbo.API.Models.Constants;

namespace Cbo.API.Models.Domain;

public class Tournament
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required TournamentStage CurrentStage { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public required int ParticipantsPerTournament { get; set; }
    public required int TopicsPerParticipantMax { get; set; }
    public required int TopicsPerParticipantMin { get; set; }
    public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = [];
    public ICollection<TournamentTopic> TournamentTopics { get; set; } = [];
    public ICollection<Match> Matches { get; set; } = [];
}
