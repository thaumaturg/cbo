using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public class PostgresTopicRepository : ITopicRepository
{
    private readonly CboDbContext _dbContext;

    public PostgresTopicRepository(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Topic>> GetAllAsync()
    {
        return await _dbContext.Topics.ToListAsync();
    }

    public async Task<Topic?> GetByIdAsync(int id)
    {
        return await _dbContext.Topics.FirstOrDefaultAsync(x => x.Id == id);
    }
    public async Task<Topic?> GetByIdIncludeQuestionsAsync(int id)
    {
        return await _dbContext.Topics.Include(t => t.Questions).FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Topic> CreateAsync(Topic topic)
    {
        await _dbContext.Topics.AddAsync(topic);
        await _dbContext.SaveChangesAsync();
        return topic;
    }

    public async Task<Topic?> UpdateAsync(int id, Topic updatedTopic)
    {
        Topic? existingTopic = await _dbContext.Topics.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTopic is null)
            return null;

        existingTopic.Title = updatedTopic.Title;
        existingTopic.PriorityIndex = updatedTopic.PriorityIndex;
        existingTopic.IsGuest = updatedTopic.IsGuest;
        existingTopic.IsPlayed = updatedTopic.IsPlayed;

        await _dbContext.SaveChangesAsync();

        return existingTopic;
    }

    public async Task<Topic?> DeleteAsync(int id)
    {
        Topic? existingTopic = await _dbContext.Topics.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTopic is null)
            return null;

        _dbContext.Topics.Remove(existingTopic);
        await _dbContext.SaveChangesAsync();

        return existingTopic;
    }
}
