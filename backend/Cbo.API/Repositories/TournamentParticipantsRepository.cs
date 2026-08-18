using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public interface ITournamentParticipantsRepository
{
    Task<List<TournamentParticipant>> GetAllByTournamentIdAsync(Guid tournamentId, TournamentParticipantRole? role = null);
    Task<TournamentParticipant?> GetByParticipantIdAndTournamentIdAsync(Guid participantId, Guid tournamentId);
    Task<TournamentParticipant?> GetByUserIdAndTournamentIdAsync(Guid userId, Guid tournamentId);
    Task<TournamentParticipant> CreateAsync(TournamentParticipant tournamentParticipant);
    Task<TournamentParticipant?> UpdateAsync(Guid id, UpdateTournamentParticipantParameters parameters);
    Task<TournamentParticipant?> DeleteAsync(Guid id);
    Task<List<TournamentParticipant>> GetAllByTournamentIdWithMatchDataAsync(Guid tournamentId);
    Task UpdateParticipantsAsync(List<TournamentParticipant> participants);
    Task<List<TournamentParticipant>> UpdateSeedsAsync(Guid tournamentId, List<Guid> orderedParticipantIds);
}

public class TournamentParticipantsRepository(CboDbContext dbContext) : ITournamentParticipantsRepository
{
    private readonly CboDbContext _dbContext = dbContext;

    public async Task<List<TournamentParticipant>> GetAllByTournamentIdAsync(Guid tournamentId, TournamentParticipantRole? role = null)
    {
        IQueryable<TournamentParticipant> query = _dbContext.TournamentParticipants
            .AsNoTracking()
            .Include(tp => tp.ApplicationUser)
            .Include(tp => tp.TournamentTopics)
            .Where(tp => tp.TournamentId == tournamentId);

        if (role.HasValue)
            query = query.Where(tp => tp.Role == role.Value);

        return await query.ToListAsync();
    }

    public async Task<TournamentParticipant?> GetByParticipantIdAndTournamentIdAsync(Guid participantId, Guid tournamentId)
    {
        return await _dbContext.TournamentParticipants
            .AsNoTracking()
            .Include(tp => tp.ApplicationUser)
            .Include(tp => tp.TournamentTopics)
            .FirstOrDefaultAsync(tp => tp.Id == participantId && tp.TournamentId == tournamentId);
    }

    public async Task<TournamentParticipant?> GetByUserIdAndTournamentIdAsync(Guid userId, Guid tournamentId)
    {
        return await _dbContext.TournamentParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.ApplicationUserId == userId && tp.TournamentId == tournamentId);
    }

    public async Task<TournamentParticipant> CreateAsync(TournamentParticipant tournamentParticipant)
    {
        await _dbContext.TournamentParticipants.AddAsync(tournamentParticipant);
        await _dbContext.SaveChangesAsync();

        return await _dbContext.TournamentParticipants
            .Include(tp => tp.ApplicationUser)
            .Include(tp => tp.TournamentTopics)
            .FirstAsync(tp => tp.Id == tournamentParticipant.Id);
    }

    public async Task<TournamentParticipant?> UpdateAsync(Guid id, UpdateTournamentParticipantParameters parameters)
    {
        TournamentParticipant? existing = await _dbContext.TournamentParticipants.FirstOrDefaultAsync(tp => tp.Id == id);

        if (existing is null)
            return null;

        existing.Role = parameters.Role;
        existing.Seed = parameters.Seed;

        await _dbContext.SaveChangesAsync();

        return await _dbContext.TournamentParticipants
            .Include(tp => tp.ApplicationUser)
            .Include(tp => tp.TournamentTopics)
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

    public async Task<List<TournamentParticipant>> UpdateSeedsAsync(Guid tournamentId, List<Guid> orderedParticipantIds)
    {
        List<TournamentParticipant> participants = await _dbContext.TournamentParticipants
            .Include(tp => tp.ApplicationUser)
            .Include(tp => tp.TournamentTopics)
            .Where(tp => tp.TournamentId == tournamentId && orderedParticipantIds.Contains(tp.Id))
            .ToListAsync();

        Dictionary<Guid, TournamentParticipant> participantsById = participants.ToDictionary(tp => tp.Id);

        for (int position = 0; position < orderedParticipantIds.Count; position++)
        {
            if (participantsById.TryGetValue(orderedParticipantIds[position], out TournamentParticipant? participant))
                participant.Seed = position + 1;
        }

        await _dbContext.SaveChangesAsync();

        return participants.OrderBy(tp => tp.Seed).ToList();
    }
}
