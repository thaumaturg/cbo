using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface IRoundRepository
{
    Task<Round?> GetByIdWithDetailsAsync(int id);
    Task<List<Round>> GetAllByTournamentIdAsync(int tournamentId);
    Task<Round?> GetByMatchIdAndNumberAsync(int matchId, int numberInMatch);
    Task<Round> CreateAsync(Round round);
    Task<Round?> DeleteAsync(int id);
    Task DeleteAnswersByRoundIdAsync(int roundId);
    Task CreateAnswersAsync(List<RoundAnswer> answers);
}
