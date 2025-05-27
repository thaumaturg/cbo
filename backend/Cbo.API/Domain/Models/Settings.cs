namespace Cbo.API.Domain.Models;

public class Settings
{
    public int Id { get; set; }
    public int ParticipantsPerMatchMax { get; set; }
    public int ParticipantsPerTournamentMax { get; set; }
    public int ParticipantsPerTournamentMin { get; set; }
    public int QuestionsCostMax { get; set; }
    public int QuestionsCostMin { get; set; }
    public int QuestionsPerTopicMax { get; set; }
    public int QuestionsPerTopicMin { get; set; }
    public int RoundsPerMatch { get; set; }
    public int TopicsAuthorsMax { get; set; }
    public int TopicsPerParticipantMax { get; set; }
    public int TopicsPerParticipantMin { get; set; }
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; }
} 
