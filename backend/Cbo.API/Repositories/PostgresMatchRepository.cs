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

    public async Task<List<Match>> GetAllAsync()
    {
        return await _dbContext.Matches.ToListAsync();
    }

    public async Task<Match?> GetByIdAsync(int id)
    {
        return await _dbContext.Matches.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Match>> CreateBulkAsync(List<Match> matches)
    {
        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();
        return matches;
    }
}
