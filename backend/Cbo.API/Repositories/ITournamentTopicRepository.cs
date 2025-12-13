using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITournamentTopicRepository
{
    Task<List<TournamentTopic>> GetAllByParticipantIdAsync(int tournamentId, int participantId);
    Task<List<TournamentTopic>> GetAllByTournamentIdAsync(int tournamentId);
    Task<List<TournamentTopic>> SetTopicsForParticipantAsync(int tournamentId, int participantId, List<TournamentTopic> topics);
}
