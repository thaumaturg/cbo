using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITournamentRepository
{
    Task<List<Tournament>> GetAllByUserIdAsync(int userId);
    Task<Tournament?> GetByIdAsync(int id);
    Task<Tournament> CreateAsync(Tournament tournament);
    Task<Tournament?> UpdateAsync(int id, Tournament updatedTournament);
    Task<Tournament?> DeleteAsync(int id);
}
