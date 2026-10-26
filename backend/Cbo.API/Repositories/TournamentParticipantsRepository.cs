using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public interface ITournamentParticipantsRepository
{
    Task<List<TournamentParticipant>> GetAllByTournamentIdWithMatchDataAsync(Guid tournamentId);
    Task UpdateParticipantsAsync(List<TournamentParticipant> participants);
}

public class TournamentParticipantsRepository(CboDbContext dbContext) : ITournamentParticipantsRepository
{
    private readonly CboDbContext _dbContext = dbContext;

    public async Task<List<TournamentParticipant>> GetAllByTournamentIdWithMatchDataAsync(Guid tournamentId)
    {
        return await _dbContext.TournamentParticipants
            .Include(tp => tp.ApplicationUser)
            .Include(tp => tp.MatchParticipants)
            .Where(tp => tp.TournamentId == tournamentId)
            .ToListAsync();
    }

    public async Task UpdateParticipantsAsync(List<TournamentParticipant> participants)
    {
        _dbContext.TournamentParticipants.UpdateRange(participants);
        await _dbContext.SaveChangesAsync();
    }
}
