using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public interface IRoundRepository
{
    Task<Round?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Round>> GetAllByTournamentIdAsync(Guid tournamentId);
    Task<Round?> GetByMatchIdAndNumberAsync(Guid matchId, int numberInMatch, CancellationToken cancellationToken = default);
    Task<Round> CreateAsync(Round round, CancellationToken cancellationToken = default);
    Task<Round?> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAnswersByRoundIdAsync(Guid roundId, CancellationToken cancellationToken = default);
    Task CreateAnswersAsync(List<RoundAnswer> answers, CancellationToken cancellationToken = default);
}

public class RoundRepository(CboDbContext dbContext) : IRoundRepository
{
    private readonly CboDbContext _dbContext = dbContext;

    public async Task<Round?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Rounds
            .AsNoTracking()
            .Include(r => r.Topic)
                .ThenInclude(t => t.Questions.OrderBy(q => q.QuestionNumber))
            .Include(r => r.RoundAnswers)
            .Include(r => r.Match)
                .ThenInclude(m => m.Tournament)
                    .ThenInclude(t => t.TournamentTopics)
                        .ThenInclude(tt => tt.TournamentParticipant)
                            .ThenInclude(tp => tp.ApplicationUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<Round>> GetAllByTournamentIdAsync(Guid tournamentId)
    {
        return await _dbContext.Rounds
            .AsNoTracking()
            .Include(r => r.Match)
            .Where(r => r.Match.TournamentId == tournamentId)
            .ToListAsync();
    }

    public async Task<Round?> GetByMatchIdAndNumberAsync(Guid matchId, int numberInMatch, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Rounds
            .AsNoTracking()
            .Include(r => r.RoundAnswers)
            .FirstOrDefaultAsync(r => r.MatchId == matchId && r.NumberInMatch == numberInMatch, cancellationToken);
    }

    public async Task<Round> CreateAsync(Round round, CancellationToken cancellationToken = default)
    {
        await _dbContext.Rounds.AddAsync(round, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return round;
    }

    public async Task DeleteAnswersByRoundIdAsync(Guid roundId, CancellationToken cancellationToken = default)
    {
        var answers = await _dbContext.RoundAnswers
            .Where(ra => ra.RoundId == roundId)
            .ToListAsync(cancellationToken);

        _dbContext.RoundAnswers.RemoveRange(answers);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateAnswersAsync(List<RoundAnswer> answers, CancellationToken cancellationToken = default)
    {
        if (answers.Count > 0)
        {
            await _dbContext.RoundAnswers.AddRangeAsync(answers, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Round?> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Round? existingRound = await _dbContext.Rounds.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (existingRound is null)
            return null;

        _dbContext.Rounds.Remove(existingRound);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return existingRound;
    }
}
