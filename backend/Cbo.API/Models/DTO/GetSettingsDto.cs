namespace Cbo.API.Models.DTO;

public class GetSettingsDto
{
    public required int Id { get; set; }
    public required int ParticipantsPerMatch { get; set; }
    public required int ParticipantsPerTournament { get; set; }
    public required int QuestionsCostMax { get; set; }
    public required int QuestionsCostMin { get; set; }
    public required int QuestionsPerTopicMax { get; set; }
    public required int QuestionsPerTopicMin { get; set; }
    public required int TopicsAuthorsMax { get; set; }
    public required int TopicsPerParticipantMax { get; set; }
    public required int TopicsPerParticipantMin { get; set; }
    public required int TopicsPerMatch { get; set; }
    public required int TournamentId { get; set; }
}
