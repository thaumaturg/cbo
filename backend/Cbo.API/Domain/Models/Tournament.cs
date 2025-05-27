using Cbo.API.Domain.Constants;

namespace Cbo.API.Domain.Models;

public class Tournament
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public TournamentStage CurrentStage { get; set; }
    public DateTime PlannedStart { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public ICollection<TournamentParticipant> TournamentParticipants { get; set; }
    public ICollection<TournamentTopic> TournamentTopics { get; set; }
    public ICollection<Match> Matches { get; set; }
    public Settings Settings { get; set; }
} 
