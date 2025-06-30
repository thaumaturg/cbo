using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public class PostgresRoundAnswerRepository : IRoundAnswerRepository
{
    private readonly CboDbContext _dbContext;

    public PostgresRoundAnswerRepository(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<RoundAnswer>> GetAllAsync()
    {
        return await _dbContext.RoundAnswers.ToListAsync();
    }

    public async Task<RoundAnswer?> GetByIdAsync(int id)
    {
        return await _dbContext.RoundAnswers.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<RoundAnswer?> GetByIdIncludeAsync(int id)
    {
        return await _dbContext.RoundAnswers
            .Include(ra => ra.Round)
            .Include(ra => ra.Question)
            .Include(ra => ra.MatchParticipant)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<RoundAnswer> CreateAsync(RoundAnswer roundAnswer)
    {
        await _dbContext.RoundAnswers.AddAsync(roundAnswer);
        await _dbContext.SaveChangesAsync();
        return roundAnswer;
    }

    public async Task<RoundAnswer?> UpdateAsync(int id, RoundAnswer updatedRoundAnswer)
    {
        RoundAnswer? existingRoundAnswer = await _dbContext.RoundAnswers.FirstOrDefaultAsync(x => x.Id == id);

        if (existingRoundAnswer is null)
            return null;

        existingRoundAnswer.IsAnswerAccepted = updatedRoundAnswer.IsAnswerAccepted;
        existingRoundAnswer.RoundId = updatedRoundAnswer.RoundId;
        existingRoundAnswer.QuestionId = updatedRoundAnswer.QuestionId;
        existingRoundAnswer.MatchParticipantId = updatedRoundAnswer.MatchParticipantId;

        await _dbContext.SaveChangesAsync();

        return existingRoundAnswer;
    }
    public async Task<RoundAnswer?> DeleteAsync(int id)
    {
        RoundAnswer? existingRoundAnswer = await _dbContext.RoundAnswers.FirstOrDefaultAsync(x => x.Id == id);

        if (existingRoundAnswer is null)
            return null;

        _dbContext.RoundAnswers.Remove(existingRoundAnswer);
        await _dbContext.SaveChangesAsync();

        return existingRoundAnswer;
    }
}
