using System.Collections.Generic;

namespace Cbo.API.Domain.Constants
{
    public static class DefaultSettings
    {
        public static readonly Dictionary<string, int> TournamentSettings = new Dictionary<string, int>
        {
            { "ParticipantsPerMatchMax", 4 },
            { "ParticipantsPerTournamentMax", 20 },
            { "ParticipantsPerTournamentMin", 8 },
            { "QuestionsCostMax", 100 },
            { "QuestionsCostMin", -100 },
            { "QuestionsPerTopicMax", 5 },
            { "QuestionsPerTopicMin", 5 },
            { "TopicsAuthorsMax", 5 },
            { "TopicsPerParticipantMax", 10 },
            { "TopicsPerParticipantMin", 6 },
            { "TopicsPerMatch", 4 }
        };
    }
}
