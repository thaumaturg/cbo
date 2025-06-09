using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface IMatchRepository
{
    Task<List<Match>> GetAllAsync();
    Task<Match?> GetByIdAsync(int id);
}
