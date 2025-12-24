using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface IMatchRepository
{
    Task<List<Match>> GetAllAsync();
    Task<Match?> GetByIdAsync(int id);
    Task<Match?> GetByIdWithDetailsAsync(int id);
    Task<List<Match>> GetAllByTournamentIdAsync(int tournamentId);
    Task<List<Match>> CreateBulkAsync(List<Match> matches);
}
