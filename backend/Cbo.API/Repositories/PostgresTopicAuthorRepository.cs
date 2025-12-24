using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public class PostgresTopicAuthorRepository : ITopicAuthorRepository
{
    private readonly CboDbContext _dbContext;

    public PostgresTopicAuthorRepository(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TopicAuthor>> GetAllByTopicIdAsync(int topicId)
    {
        return await _dbContext.TopicAuthors
            .Include(ta => ta.ApplicationUser)
            .Where(ta => ta.TopicId == topicId)
            .ToListAsync();
    }

    public async Task<TopicAuthor?> GetByAuthorIdAndTopicIdAsync(int authorId, int topicId)
    {
        return await _dbContext.TopicAuthors
            .Include(ta => ta.ApplicationUser)
            .FirstOrDefaultAsync(ta => ta.Id == authorId && ta.TopicId == topicId);
    }

    public async Task<TopicAuthor?> GetByUserIdAndTopicIdAsync(int userId, int topicId)
    {
        return await _dbContext.TopicAuthors
            .FirstOrDefaultAsync(ta => ta.ApplicationUserId == userId && ta.TopicId == topicId);
    }

    public async Task<TopicAuthor> CreateAsync(TopicAuthor topicAuthor)
    {
        await _dbContext.TopicAuthors.AddAsync(topicAuthor);
        await _dbContext.SaveChangesAsync();

        return await _dbContext.TopicAuthors
            .Include(ta => ta.ApplicationUser)
            .FirstAsync(ta => ta.Id == topicAuthor.Id);
    }

    public async Task<TopicAuthor?> DeleteAsync(int id)
    {
        TopicAuthor? existing = await _dbContext.TopicAuthors
            .Include(ta => ta.ApplicationUser)
            .FirstOrDefaultAsync(ta => ta.Id == id);

        if (existing is null)
            return null;

        _dbContext.TopicAuthors.Remove(existing);
        await _dbContext.SaveChangesAsync();

        return existing;
    }
}
