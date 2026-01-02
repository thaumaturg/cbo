using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public class PostgresTournamentTopicRepository : ITournamentTopicRepository
{
    private readonly CboDbContext _dbContext;

    public PostgresTournamentTopicRepository(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TournamentTopic>> GetAllByParticipantIdAsync(Guid tournamentId, Guid participantId)
    {
        return await _dbContext.TournamentTopics
            .AsNoTracking()
            .Include(tt => tt.Topic)
            .Include(tt => tt.TournamentParticipant)
                .ThenInclude(tp => tp.ApplicationUser)
            .Where(tt => tt.TournamentId == tournamentId && tt.TournamentParticipantId == participantId)
            .OrderBy(tt => tt.PriorityIndex)
            .ToListAsync();
    }

    public async Task<List<TournamentTopic>> GetAllByTournamentIdAsync(Guid tournamentId)
    {
        return await _dbContext.TournamentTopics
            .AsNoTracking()
            .Include(tt => tt.Topic)
            .Include(tt => tt.TournamentParticipant)
                .ThenInclude(tp => tp.ApplicationUser)
            .Where(tt => tt.TournamentId == tournamentId)
            .OrderBy(tt => tt.TournamentParticipantId)
            .ThenBy(tt => tt.PriorityIndex)
            .ToListAsync();
    }

    public async Task<List<TournamentTopic>> GetAllByTournamentIdWithAuthorsAsync(Guid tournamentId)
    {
        return await _dbContext.TournamentTopics
            .AsNoTracking()
            .Include(tt => tt.Topic)
                .ThenInclude(t => t.TopicAuthors)
            .Include(tt => tt.TournamentParticipant)
                .ThenInclude(tp => tp.ApplicationUser)
            .Where(tt => tt.TournamentId == tournamentId)
            .OrderBy(tt => tt.PriorityIndex)
            .ToListAsync();
    }

    public async Task<List<TournamentTopic>> SetTopicsForParticipantAsync(Guid tournamentId, Guid participantId, List<TournamentTopic> topics)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            List<TournamentTopic> existingTopics = await _dbContext.TournamentTopics
                .Where(tt => tt.TournamentId == tournamentId && tt.TournamentParticipantId == participantId)
                .ToListAsync();

            _dbContext.TournamentTopics.RemoveRange(existingTopics);

            if (topics.Count > 0)
                await _dbContext.TournamentTopics.AddRangeAsync(topics);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetAllByParticipantIdAsync(tournamentId, participantId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
