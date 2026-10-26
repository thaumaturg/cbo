using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public interface ITournamentTopicRepository
{
    Task<List<TournamentTopic>> GetAllByTournamentIdAsync(Guid tournamentId);
    Task<List<TournamentTopic>> GetAllByTournamentIdWithAuthorsAsync(Guid tournamentId);
}

public class TournamentTopicRepository(CboDbContext dbContext) : ITournamentTopicRepository
{
    private readonly CboDbContext _dbContext = dbContext;

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
}
