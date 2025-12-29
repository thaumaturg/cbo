using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface IRoundRepository
{
    Task<Round?> GetByIdWithDetailsAsync(Guid id);
    Task<List<Round>> GetAllByTournamentIdAsync(Guid tournamentId);
    Task<Round?> GetByMatchIdAndNumberAsync(Guid matchId, int numberInMatch);
    Task<Round> CreateAsync(Round round);
    Task<Round?> DeleteAsync(Guid id);
    Task DeleteAnswersByRoundIdAsync(Guid roundId);
    Task CreateAnswersAsync(List<RoundAnswer> answers);
}
