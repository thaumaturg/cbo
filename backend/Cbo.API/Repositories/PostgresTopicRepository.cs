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

    public async Task<List<Topic>> GetAllByUserIdAsync(int userId)
    {
        return await _dbContext.Topics
            .Include(t => t.Questions.OrderBy(q => q.QuestionNumber))
            .Include(t => t.TopicAuthors)
            .Where(t => t.TopicAuthors.Any(ta => ta.ApplicationUserId == userId && ta.IsOwner))
            .ToListAsync();
    }

    public async Task<Topic?> GetByIdAsync(int id)
    {
        return await _dbContext.Topics.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Topic?> GetByIdIncludeQuestionsAsync(int id)
    {
        return await _dbContext.Topics
            .Include(t => t.Questions.OrderBy(q => q.QuestionNumber))
            .Include(t => t.TopicAuthors)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Topic> CreateAsync(Topic topic)
    {
        await _dbContext.Topics.AddAsync(topic);
        await _dbContext.SaveChangesAsync();
        return topic;
    }

    public async Task<Topic?> UpdateAsync(int id, Topic updatedTopic, int currentUserId, bool isAuthor)
    {
        Topic? existingTopic = await _dbContext.Topics
            .Include(t => t.Questions)
            .Include(t => t.TopicAuthors)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existingTopic is null)
            return null;

        // Update basic topic properties
        existingTopic.Title = updatedTopic.Title;
        existingTopic.Description = updatedTopic.Description;
        existingTopic.IsPlayed = updatedTopic.IsPlayed;

        // Update questions - replace all existing questions with new ones
        _dbContext.Questions.RemoveRange(existingTopic.Questions);

        foreach (Question question in updatedTopic.Questions)
        {
            question.Id = 0; // Reset ID so EF creates new records
            question.TopicId = id;
            existingTopic.Questions.Add(question);
        }

        // Update TopicAuthor IsAuthor flag for current user
        TopicAuthor? topicAuthor = existingTopic.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUserId);
        if (topicAuthor is not null)
        {
            topicAuthor.IsAuthor = isAuthor;
        }

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
