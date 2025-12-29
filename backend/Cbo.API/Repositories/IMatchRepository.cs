using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(Guid id);
    Task<Match?> GetByIdWithDetailsAsync(Guid id);
    Task<List<Match>> GetAllByTournamentIdAsync(Guid tournamentId);
    Task<List<Match>> CreateBulkAsync(List<Match> matches);
    Task<Match?> GetByIdWithScoreDataAsync(Guid id);
    Task UpdateMatchParticipantsAsync(List<MatchParticipant> participants);
}
