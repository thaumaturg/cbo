using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public class PostgresTournamentRepository : ITournamentRepository
{
    private readonly CboDbContext _dbContext;

    public PostgresTournamentRepository(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Tournament>> GetAllAsync()
    {
        return await _dbContext.Tournaments.ToListAsync();
    }

    public async Task<Tournament?> GetByIdAsync(int id)
    {
        return await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Tournament> CreateAsync(Tournament tournament)
    {
        await _dbContext.Tournaments.AddAsync(tournament);
        await _dbContext.SaveChangesAsync();
        return tournament;
    }

    public async Task<Tournament?> UpdateAsync(int id, Tournament updatedTournament)
    {
        Tournament? existingTournament = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament == null)
            return null;

        existingTournament.Title = updatedTournament.Title;
        existingTournament.Description = updatedTournament.Description;
        existingTournament.PlannedStart = updatedTournament.PlannedStart;

        await _dbContext.SaveChangesAsync();

        return existingTournament;
    }

    public async Task<Tournament?> DeleteAsync(int id)
    {
        Tournament? existingTournament = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament == null)
            return null;

        _dbContext.Tournaments.Remove(existingTournament);
        await _dbContext.SaveChangesAsync();

        return existingTournament;
    }
}
