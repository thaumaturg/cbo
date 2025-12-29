namespace Cbo.API.Models.DTO;

public record GetTournamentTopicDto
{
    public required Guid Id { get; set; }
    public required int PriorityIndex { get; set; }
    public required Guid TournamentId { get; set; }
    public required Guid TopicId { get; set; }
    public required string TopicTitle { get; set; }
    public required Guid TournamentParticipantId { get; set; }
    public required string ParticipantUsername { get; set; }
}
