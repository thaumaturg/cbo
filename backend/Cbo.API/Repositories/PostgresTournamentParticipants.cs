using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public class PostgresTournamentParticipantsRepository : ITournamentParticipantsRepository
{
    private readonly CboDbContext _dbContext;

    public PostgresTournamentParticipantsRepository(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TournamentParticipant>> GetAllAsync()
    {
        return await _dbContext.TournamentParticipants.ToListAsync();
    }

    public async Task<TournamentParticipant?> GetByIdAsync(int id)
    {
        return await _dbContext.TournamentParticipants.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TournamentParticipant?> GetByIdIncludeAsync(int id)
    {
        return await _dbContext.TournamentParticipants
            .Include(ra => ra.Tournament)
            .Include(ra => ra.ApplicationUser)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TournamentParticipant> CreateAsync(TournamentParticipant tournamentParticipant)
    {
        await _dbContext.TournamentParticipants.AddAsync(tournamentParticipant);
        await _dbContext.SaveChangesAsync();
        return tournamentParticipant;
    }

    public async Task<TournamentParticipant?> UpdateAsync(int id, TournamentParticipant updatedTournamentParticipant)
    {
        TournamentParticipant? existingTournamentParticipant = await _dbContext.TournamentParticipants.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournamentParticipant is null)
            return null;

        existingTournamentParticipant.Role = updatedTournamentParticipant.Role;
        existingTournamentParticipant.PointsSum = updatedTournamentParticipant.PointsSum;
        existingTournamentParticipant.TournamentId = updatedTournamentParticipant.TournamentId;
        existingTournamentParticipant.ApplicationUserId = updatedTournamentParticipant.ApplicationUserId;

        await _dbContext.SaveChangesAsync();

        return existingTournamentParticipant;
    }
    public async Task<TournamentParticipant?> DeleteAsync(int id)
    {
        TournamentParticipant? existingTournamentParticipant = await _dbContext.TournamentParticipants.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournamentParticipant is null)
            return null;

        _dbContext.TournamentParticipants.Remove(existingTournamentParticipant);
        await _dbContext.SaveChangesAsync();

        return existingTournamentParticipant;
    }
}
