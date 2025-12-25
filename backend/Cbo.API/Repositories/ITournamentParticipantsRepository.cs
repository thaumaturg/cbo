using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITournamentParticipantsRepository
{
    Task<List<TournamentParticipant>> GetAllByTournamentIdAsync(int tournamentId, TournamentParticipantRole? role = null);
    Task<TournamentParticipant?> GetByParticipantIdAndTournamentIdAsync(int participantId, int tournamentId);
    Task<TournamentParticipant?> GetByUserIdAndTournamentIdAsync(int userId, int tournamentId);
    Task<TournamentParticipant> CreateAsync(TournamentParticipant tournamentParticipant);
    Task<TournamentParticipant?> UpdateAsync(int id, TournamentParticipant tournamentParticipant);
    Task<TournamentParticipant?> DeleteAsync(int id);
    Task<List<TournamentParticipant>> GetAllByTournamentIdWithMatchDataAsync(int tournamentId);
    Task UpdateParticipantsAsync(List<TournamentParticipant> participants);
}
