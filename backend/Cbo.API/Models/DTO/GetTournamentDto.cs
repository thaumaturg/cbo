using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public record GetTournamentDto
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string CurrentStage { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public required int ParticipantsPerTournament { get; set; }
    public required int QuestionsPerTopicMax { get; set; }
    public required int QuestionsPerTopicMin { get; set; }
    public required int TopicsAuthorsMax { get; set; }
    public required int TopicsPerParticipantMax { get; set; }
    public required int TopicsPerParticipantMin { get; set; }
    public required int TopicsPerMatch { get; set; }
    public TournamentParticipantRole? CurrentUserRole { get; set; }
}
