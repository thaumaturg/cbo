using Cbo.API.Data;
using Cbo.API.Models.Constants;
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

    public async Task<List<TournamentParticipant>> GetAllByTournamentIdAsync(Guid tournamentId, TournamentParticipantRole? role = null)
    {
        IQueryable<TournamentParticipant> query = _dbContext.TournamentParticipants
            .Include(tp => tp.ApplicationUser)
            .Where(tp => tp.TournamentId == tournamentId);

        if (role.HasValue)
            query = query.Where(tp => tp.Role == role.Value);

        return await query.ToListAsync();
    }

    public async Task<TournamentParticipant?> GetByParticipantIdAndTournamentIdAsync(Guid participantId, Guid tournamentId)
    {
        return await _dbContext.TournamentParticipants
            .Include(tp => tp.ApplicationUser)
            .FirstOrDefaultAsync(tp => tp.Id == participantId && tp.TournamentId == tournamentId);
    }

    public async Task<TournamentParticipant?> GetByUserIdAndTournamentIdAsync(Guid userId, Guid tournamentId)
    {
        return await _dbContext.TournamentParticipants
            .FirstOrDefaultAsync(tp => tp.ApplicationUserId == userId && tp.TournamentId == tournamentId);
    }

    public async Task<TournamentParticipant> CreateAsync(TournamentParticipant tournamentParticipant)
    {
        await _dbContext.TournamentParticipants.AddAsync(tournamentParticipant);
        await _dbContext.SaveChangesAsync();

        return await _dbContext.TournamentParticipants
            .Include(tp => tp.ApplicationUser)
            .FirstAsync(tp => tp.Id == tournamentParticipant.Id);
    }

    public async Task<TournamentParticipant?> UpdateAsync(Guid id, TournamentParticipant tournamentParticipant)
    {
        TournamentParticipant? existing = await _dbContext.TournamentParticipants.FirstOrDefaultAsync(tp => tp.Id == id);

        if (existing is null)
            return null;

        existing.Role = tournamentParticipant.Role;
        existing.PointsSum = tournamentParticipant.PointsSum;

        await _dbContext.SaveChangesAsync();

        return await _dbContext.TournamentParticipants
            .Include(tp => tp.ApplicationUser)
            .FirstAsync(tp => tp.Id == id);
    }

    public async Task<TournamentParticipant?> DeleteAsync(Guid id)
    {
        TournamentParticipant? existing = await _dbContext.TournamentParticipants
            .Include(tp => tp.ApplicationUser)
            .FirstOrDefaultAsync(tp => tp.Id == id);

        if (existing is null)
            return null;

        _dbContext.TournamentParticipants.Remove(existing);
        await _dbContext.SaveChangesAsync();

        return existing;
    }

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
