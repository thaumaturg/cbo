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

    public async Task<List<Tournament>> GetAllByUserIdAsync(int userId)
    {
        return await _dbContext.Tournaments
            .Where(t => t.TournamentParticipants.Any(tp => tp.ApplicationUserId == userId))
            .ToListAsync();
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

        if (existingTournament is null)
            return null;

        existingTournament.Title = updatedTournament.Title;
        existingTournament.Description = updatedTournament.Description;
        existingTournament.ParticipantsPerTournament = updatedTournament.ParticipantsPerTournament;
        existingTournament.TopicsPerParticipantMax = updatedTournament.TopicsPerParticipantMax;
        existingTournament.TopicsPerParticipantMin = updatedTournament.TopicsPerParticipantMin;

        await _dbContext.SaveChangesAsync();

        return existingTournament;
    }

    public async Task<Tournament?> UpdateStageAsync(int id, TournamentStage stage)
    {
        Tournament? existingTournament = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament is null)
            return null;

        existingTournament.CurrentStage = stage;
        existingTournament.StartedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return existingTournament;
    }

    public async Task<Tournament?> DeleteAsync(int id)
    {
        Tournament? existingTournament = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament is null)
            return null;

        _dbContext.Tournaments.Remove(existingTournament);
        await _dbContext.SaveChangesAsync();

        return existingTournament;
    }
}
