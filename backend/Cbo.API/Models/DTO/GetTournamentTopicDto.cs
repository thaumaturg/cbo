namespace Cbo.API.Models.DTO;

public record GetTournamentTopicDto
{
    public required int Id { get; set; }
    public required int PriorityIndex { get; set; }
    public required int TournamentId { get; set; }
    public required int TopicId { get; set; }
    public required string TopicTitle { get; set; }
    public required int TournamentParticipantId { get; set; }
    public required string ParticipantUsername { get; set; }
}
