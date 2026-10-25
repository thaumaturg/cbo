using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public interface ITournamentTopicRepository
{
    Task<List<TournamentTopic>> GetAllByParticipantIdAsync(Guid tournamentId, Guid participantId);
    Task<List<TournamentTopic>> GetAllByTournamentIdAsync(Guid tournamentId);
    Task<List<TournamentTopic>> GetAllByTournamentIdWithAuthorsAsync(Guid tournamentId);
    Task DeleteAllByParticipantIdAsync(Guid tournamentId, Guid participantId);
    Task CreateBulkAsync(List<TournamentTopic> topics);
}

public class TournamentTopicRepository(CboDbContext dbContext) : ITournamentTopicRepository
{
    private readonly CboDbContext _dbContext = dbContext;

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

    public async Task DeleteAllByParticipantIdAsync(Guid tournamentId, Guid participantId)
    {
        await _dbContext.TournamentTopics
            .Where(tt => tt.TournamentId == tournamentId && tt.TournamentParticipantId == participantId)
            .ExecuteDeleteAsync();
    }

    public async Task CreateBulkAsync(List<TournamentTopic> topics)
    {
        if (topics.Count > 0)
        {
            await _dbContext.TournamentTopics.AddRangeAsync(topics);
            await _dbContext.SaveChangesAsync();
        }
    }
}
