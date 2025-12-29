using Cbo.API.Data;
using Cbo.API.Models.Constants;
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

    public async Task<List<Tournament>> GetAllByUserIdAsync(Guid userId)
    {
        return await _dbContext.Tournaments
            .Where(t => t.TournamentParticipants.Any(tp => tp.ApplicationUserId == userId))
            .ToListAsync();
    }

    public async Task<Tournament?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Tournament> CreateAsync(Tournament tournament)
    {
        await _dbContext.Tournaments.AddAsync(tournament);
        await _dbContext.SaveChangesAsync();
        return tournament;
    }

    public async Task<Tournament?> UpdateAsync(Guid id, Tournament updatedTournament)
    {
        Tournament? existingTournament = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament is null)
            return null;

        existingTournament.Title = updatedTournament.Title;
        existingTournament.Description = updatedTournament.Description;
        existingTournament.PlayersPerTournament = updatedTournament.PlayersPerTournament;
        existingTournament.TopicsPerParticipantMax = updatedTournament.TopicsPerParticipantMax;
        existingTournament.TopicsPerParticipantMin = updatedTournament.TopicsPerParticipantMin;

        await _dbContext.SaveChangesAsync();

        return existingTournament;
    }

    public async Task<Tournament?> UpdateStageAsync(Guid id, TournamentStage stage)
    {
        Tournament? existingTournament = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament is null)
            return null;

        existingTournament.CurrentStage = stage;
        existingTournament.StartedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return existingTournament;
    }

    public async Task<Tournament?> DeleteAsync(Guid id)
    {
        Tournament? existingTournament = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament is null)
            return null;

        _dbContext.Tournaments.Remove(existingTournament);
        await _dbContext.SaveChangesAsync();

        return existingTournament;
    }
}
