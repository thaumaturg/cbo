namespace Cbo.API.Models.Domain;

public class TournamentTopic
{
    public Guid Id { get; set; }
    public required int PriorityIndex { get; set; }
    public required Guid TournamentId { get; set; }
    public required Tournament Tournament { get; set; }
    public required Guid TopicId { get; set; }
    public required Topic Topic { get; set; }
    public required Guid TournamentParticipantId { get; set; }
    public required TournamentParticipant TournamentParticipant { get; set; }
}
