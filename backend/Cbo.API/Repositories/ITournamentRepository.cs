using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITournamentRepository
{
    Task<List<Tournament>> GetAllByUserIdAsync(Guid userId);
    Task<Tournament?> GetByIdAsync(Guid id);
    Task<Tournament> CreateAsync(Tournament tournament);
    Task<Tournament?> UpdateAsync(Guid id, Tournament updatedTournament);
    Task<Tournament?> UpdateStageAsync(Guid id, TournamentStage stage);
    Task<Tournament?> DeleteAsync(Guid id);
}
