using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public interface ITournamentRepository
{
    Task<List<Tournament>> GetAllByUserIdAsync(Guid userId);
    Task<Tournament?> GetByIdAsync(Guid id);
    Task<Tournament> CreateAsync(Tournament tournament);
    Task<Tournament?> UpdateAsync(Guid id, UpdateTournamentParameters parameters);
    Task<Tournament?> UpdateStageAsync(Guid id, TournamentStage stage);
    Task<Tournament?> DeleteAsync(Guid id);
}

public class TournamentRepository(CboDbContext dbContext) : ITournamentRepository
{
    private readonly CboDbContext _dbContext = dbContext;

    public async Task<List<Tournament>> GetAllByUserIdAsync(Guid userId)
    {
        return await _dbContext.Tournaments
            .AsNoTracking()
            .Where(t => t.TournamentParticipants.Any(tp => tp.ApplicationUserId == userId))
            .Include(t => t.TournamentParticipants.Where(tp => tp.ApplicationUserId == userId))
            .ToListAsync();
    }

    public async Task<Tournament?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Tournament> CreateAsync(Tournament tournament)
    {
        await _dbContext.Tournaments.AddAsync(tournament);
        await _dbContext.SaveChangesAsync();
        return tournament;
    }

    public async Task<Tournament?> UpdateAsync(Guid id, UpdateTournamentParameters parameters)
    {
        Tournament? existingTournament = await _dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTournament is null)
            return null;

        existingTournament.Title = parameters.Title;
        existingTournament.Description = parameters.Description;
        existingTournament.PlayersPerTournament = parameters.PlayersPerTournament;
        existingTournament.TopicsPerParticipantMax = parameters.TopicsPerParticipantMax;
        existingTournament.TopicsPerParticipantMin = parameters.TopicsPerParticipantMin;

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

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        await _dbContext.RoundAnswers
            .Where(ra => ra.Round.Match.TournamentId == id)
            .ExecuteDeleteAsync();

        await _dbContext.MatchParticipants
            .Where(mp => mp.Match.TournamentId == id)
            .ExecuteDeleteAsync();

        _dbContext.Tournaments.Remove(existingTournament);
        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return existingTournament;
    }
}
