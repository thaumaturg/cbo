namespace Cbo.API.Models.Constants;

public static class DefaultSettings
{
    public static readonly Dictionary<string, int> TournamentSettings = new Dictionary<string, int>
    {
        { "ParticipantsPerMatch", 4 },
        { "ParticipantsPerTournament", 12 },
        { "QuestionsCostMax", 50 },
        { "QuestionsCostMin", -50 },
        { "QuestionsPerTopicMax", 5 },
        { "QuestionsPerTopicMin", 5 },
        { "TopicsAuthorsMax", 5 },
        { "TopicsPerParticipantMax", 10 },
        { "TopicsPerParticipantMin", 6 },
        { "TopicsPerMatch", 4 }
    };
}
