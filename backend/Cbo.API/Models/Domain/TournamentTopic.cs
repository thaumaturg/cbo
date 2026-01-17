namespace Cbo.API.Models.Domain;

public class TournamentTopic
{
    public Guid Id { get; set; }
    public required int PriorityIndex { get; set; }
    public required Guid TournamentId { get; set; }
    public required Guid TopicId { get; set; }
    public required Guid TournamentParticipantId { get; set; }

    // Navigation properties
    public Tournament? Tournament { get; set; }
    public Topic? Topic { get; set; }
    public TournamentParticipant? TournamentParticipant { get; set; }
}
