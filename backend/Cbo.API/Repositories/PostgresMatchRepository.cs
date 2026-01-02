using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public class PostgresMatchRepository : IMatchRepository
{
    private readonly CboDbContext _dbContext;

    public PostgresMatchRepository(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Match?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Match?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbContext.Matches
            .AsNoTracking()
            .Include(m => m.MatchParticipants)
                .ThenInclude(mp => mp.TournamentParticipant)
                    .ThenInclude(tp => tp.ApplicationUser)
            .Include(m => m.Rounds)
                .ThenInclude(r => r.Topic)
                    .ThenInclude(t => t.Questions)
            .Include(m => m.Rounds)
                .ThenInclude(r => r.RoundAnswers)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Match?> GetByIdWithParticipantsAsync(Guid id)
    {
        return await _dbContext.Matches
            .AsNoTracking()
            .Include(m => m.MatchParticipants)
                .ThenInclude(mp => mp.TournamentParticipant)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Match>> GetAllByTournamentIdAsync(Guid tournamentId)
    {
        return await _dbContext.Matches
            .AsNoTracking()
            .Where(m => m.TournamentId == tournamentId)
            .Include(m => m.MatchParticipants)
                .ThenInclude(mp => mp.TournamentParticipant)
                    .ThenInclude(tp => tp.ApplicationUser)
            .Include(m => m.Rounds)
            .OrderBy(m => m.NumberInTournament)
            .ToListAsync();
    }

    public async Task<List<Match>> CreateBulkAsync(List<Match> matches)
    {
        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();
        return matches;
    }

    public async Task<Match?> GetByIdWithScoreDataAsync(Guid id)
    {
        return await _dbContext.Matches
            .Include(m => m.MatchParticipants)
                .ThenInclude(mp => mp.RoundAnswers)
                    .ThenInclude(ra => ra.Question)
            .Include(m => m.Rounds)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task UpdateMatchParticipantsAsync(List<MatchParticipant> participants)
    {
        _dbContext.MatchParticipants.UpdateRange(participants);
        await _dbContext.SaveChangesAsync();
    }
}
