using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITournamentParticipantsRepository
{
    Task<List<TournamentParticipant>> GetAllAsync();
    Task<TournamentParticipant?> GetByIdAsync(int id);
    Task<TournamentParticipant?> GetByIdIncludeAsync(int id);
    Task<TournamentParticipant> CreateAsync(TournamentParticipant tournamentParticipant);
    Task<TournamentParticipant?> UpdateAsync(int id, TournamentParticipant tournamentParticipant);
    Task<TournamentParticipant?> DeleteAsync(int id);
}

