using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public class PostgresRoundRepository : IRoundRepository
{
    private readonly CboDbContext _dbContext;

    public PostgresRoundRepository(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Round>> GetAllAsync()
    {
        return await _dbContext.Rounds.ToListAsync();
    }

    public async Task<Round?> GetByIdAsync(int id)
    {
        return await _dbContext.Rounds.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Round> CreateAsync(Round round)
    {
        await _dbContext.Rounds.AddAsync(round);
        await _dbContext.SaveChangesAsync();
        return round;
    }

    public async Task<Round?> UpdateAsync(int id, Round updatedRound)
    {
        Round? existingRound = await _dbContext.Rounds.FirstOrDefaultAsync(x => x.Id == id);

        if (existingRound is null)
            return null;

        existingRound.NumberInMatch = updatedRound.NumberInMatch;
        existingRound.TopicId = updatedRound.TopicId;
        existingRound.MatchId = updatedRound.MatchId;

        await _dbContext.SaveChangesAsync();

        return existingRound;
    }

    public async Task<Round?> DeleteAsync(int id)
    {
        Round? existingRound = await _dbContext.Rounds.FirstOrDefaultAsync(x => x.Id == id);

        if (existingRound is null)
            return null;

        _dbContext.Rounds.Remove(existingRound);
        await _dbContext.SaveChangesAsync();

        return existingRound;
    }
}
