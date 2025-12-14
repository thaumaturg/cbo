namespace Cbo.API.Models.Domain;

public class TournamentTopic
{
    public int Id { get; set; }
    public required int PriorityIndex { get; set; }
    public required int TournamentId { get; set; }
    public required Tournament Tournament { get; set; }
    public required int TopicId { get; set; }
    public required Topic Topic { get; set; }
    public required int TournamentParticipantId { get; set; }
    public required TournamentParticipant TournamentParticipant { get; set; }
}
