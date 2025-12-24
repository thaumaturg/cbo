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

    public async Task<Round?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbContext.Rounds
            .Include(r => r.Topic)
                .ThenInclude(t => t.Questions.OrderBy(q => q.QuestionNumber))
            .Include(r => r.RoundAnswers)
            .Include(r => r.Match)
                .ThenInclude(m => m.Tournament)
                    .ThenInclude(t => t.TournamentTopics)
                        .ThenInclude(tt => tt.TournamentParticipant)
                            .ThenInclude(tp => tp.ApplicationUser)
            .FirstOrDefaultAsync(x => x.Id == id);
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
