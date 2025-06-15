using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface IRoundRepository
{
    Task<List<Round>> GetAllAsync();
    Task<Round?> GetByIdAsync(int id);
    Task<Round> CreateAsync(Round round);
    Task<Round?> UpdateAsync(int id, Round updatedRound);
    Task<Round?> DeleteAsync(int id);
}
