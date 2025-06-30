using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface IRoundAnswerRepository
{
    Task<List<RoundAnswer>> GetAllAsync();
    Task<RoundAnswer?> GetByIdAsync(int id);
    Task<RoundAnswer> CreateAsync(RoundAnswer roundAnswer);
    Task<RoundAnswer?> UpdateAsync(int id, RoundAnswer updatedRoundAnswer);
    Task<RoundAnswer?> DeleteAsync(int id);
}
