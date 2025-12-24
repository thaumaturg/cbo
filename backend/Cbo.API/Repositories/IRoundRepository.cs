using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface IRoundRepository
{
    Task<List<Round>> GetAllAsync();
    Task<Round?> GetByIdAsync(int id);
    Task<List<Round>> GetAllByTournamentIdAsync(int tournamentId);
    Task<Round?> GetByMatchIdAndNumberAsync(int matchId, int numberInMatch);
    Task<Round> CreateAsync(Round round);
    Task<Round> CreateWithAnswersAsync(Round round, List<RoundAnswer> answers);
    Task<Round?> UpdateAsync(int id, Round updatedRound);
    Task<Round?> DeleteAsync(int id);
    Task DeleteAnswersByRoundIdAsync(int roundId);
    Task CreateAnswersAsync(List<RoundAnswer> answers);
}
