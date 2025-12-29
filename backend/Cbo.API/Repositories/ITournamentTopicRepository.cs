using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITournamentTopicRepository
{
    Task<List<TournamentTopic>> GetAllByParticipantIdAsync(Guid tournamentId, Guid participantId);
    Task<List<TournamentTopic>> GetAllByTournamentIdAsync(Guid tournamentId);
    Task<List<TournamentTopic>> SetTopicsForParticipantAsync(Guid tournamentId, Guid participantId, List<TournamentTopic> topics);
}
