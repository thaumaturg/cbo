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

    public async Task<List<Round>> GetAllByTournamentIdAsync(int tournamentId)
    {
        return await _dbContext.Rounds
            .Include(r => r.Match)
            .Where(r => r.Match.TournamentId == tournamentId)
            .ToListAsync();
    }

    public async Task<Round?> GetByMatchIdAndNumberAsync(int matchId, int numberInMatch)
    {
        return await _dbContext.Rounds
            .Include(r => r.RoundAnswers)
            .FirstOrDefaultAsync(r => r.MatchId == matchId && r.NumberInMatch == numberInMatch);
    }

    public async Task<Round> CreateAsync(Round round)
    {
        await _dbContext.Rounds.AddAsync(round);
        await _dbContext.SaveChangesAsync();
        return round;
    }

    public async Task<Round> CreateWithAnswersAsync(Round round, List<RoundAnswer> answers)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            await _dbContext.Rounds.AddAsync(round);
            await _dbContext.SaveChangesAsync();

            foreach (var answer in answers)
            {
                answer.RoundId = round.Id;
            }

            if (answers.Count > 0)
            {
                await _dbContext.RoundAnswers.AddRangeAsync(answers);
                await _dbContext.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return round;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAnswersByRoundIdAsync(int roundId)
    {
        var answers = await _dbContext.RoundAnswers
            .Where(ra => ra.RoundId == roundId)
            .ToListAsync();

        _dbContext.RoundAnswers.RemoveRange(answers);
        await _dbContext.SaveChangesAsync();
    }

    public async Task CreateAnswersAsync(List<RoundAnswer> answers)
    {
        if (answers.Count > 0)
        {
            await _dbContext.RoundAnswers.AddRangeAsync(answers);
            await _dbContext.SaveChangesAsync();
        }
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
