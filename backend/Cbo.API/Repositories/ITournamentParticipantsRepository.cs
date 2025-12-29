using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITournamentParticipantsRepository
{
    Task<List<TournamentParticipant>> GetAllByTournamentIdAsync(Guid tournamentId, TournamentParticipantRole? role = null);
    Task<TournamentParticipant?> GetByParticipantIdAndTournamentIdAsync(Guid participantId, Guid tournamentId);
    Task<TournamentParticipant?> GetByUserIdAndTournamentIdAsync(Guid userId, Guid tournamentId);
    Task<TournamentParticipant> CreateAsync(TournamentParticipant tournamentParticipant);
    Task<TournamentParticipant?> UpdateAsync(Guid id, TournamentParticipant tournamentParticipant);
    Task<TournamentParticipant?> DeleteAsync(Guid id);
    Task<List<TournamentParticipant>> GetAllByTournamentIdWithMatchDataAsync(Guid tournamentId);
    Task UpdateParticipantsAsync(List<TournamentParticipant> participants);
}
