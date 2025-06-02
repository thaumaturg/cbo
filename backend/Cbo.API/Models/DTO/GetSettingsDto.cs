namespace Cbo.API.Models.DTO;

public class GetSettingsDto
{
    public int Id { get; set; }
    public int ParticipantsPerMatch { get; set; }
    public int ParticipantsPerTournament { get; set; }
    public int QuestionsCostMax { get; set; }
    public int QuestionsCostMin { get; set; }
    public int QuestionsPerTopicMax { get; set; }
    public int QuestionsPerTopicMin { get; set; }
    public int TopicsAuthorsMax { get; set; }
    public int TopicsPerParticipantMax { get; set; }
    public int TopicsPerParticipantMin { get; set; }
    public int TopicsPerMatch { get; set; }
    public int TournamentId { get; set; }
}
