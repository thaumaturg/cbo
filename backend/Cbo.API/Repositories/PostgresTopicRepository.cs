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

    public async Task<List<Topic>> GetAllByUserIdAsync(Guid userId)
    {
        return await _dbContext.Topics
            .AsNoTracking()
            .Include(t => t.Questions.OrderBy(q => q.QuestionNumber))
            .Include(t => t.TopicAuthors)
                .ThenInclude(ta => ta.ApplicationUser)
            .Include(t => t.Rounds)
            .Where(t => t.TopicAuthors.Any(ta => ta.ApplicationUserId == userId && ta.IsOwner))
            .ToListAsync();
    }

    public async Task<Topic?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Topics
            .AsNoTracking()
            .Include(t => t.Rounds)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Topic?> GetByIdIncludeQuestionsAsync(Guid id)
    {
        return await _dbContext.Topics
            .AsNoTracking()
            .Include(t => t.Questions.OrderBy(q => q.QuestionNumber))
            .Include(t => t.TopicAuthors)
                .ThenInclude(ta => ta.ApplicationUser)
            .Include(t => t.Rounds)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Topic> CreateAsync(Topic topic)
    {
        await _dbContext.Topics.AddAsync(topic);
        await _dbContext.SaveChangesAsync();
        return topic;
    }

    public async Task<Topic?> UpdateAsync(Guid id, Topic updatedTopic, Guid currentUserId, bool isAuthor, ICollection<Question> incomingQuestions)
    {
        Topic? existingTopic = await _dbContext.Topics
            .Include(t => t.Questions)
            .Include(t => t.TopicAuthors)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existingTopic is null)
            return null;

        // Validate all existing question IDs are present
        HashSet<Guid> incomingQuestionIds = incomingQuestions.Select(q => q.Id).ToHashSet();
        HashSet<Guid> existingQuestionIds = existingTopic.Questions.Select(q => q.Id).ToHashSet();

        List<Guid> missingIds = existingQuestionIds.Except(incomingQuestionIds).ToList();
        if (missingIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Incomplete update request. Missing question IDs: {string.Join(", ", missingIds)}. " +
                $"PUT requires all existing questions to be included.");
        }

        existingTopic.Title = updatedTopic.Title;
        existingTopic.Description = updatedTopic.Description;

        foreach (Question incomingQuestion in incomingQuestions)
        {
            Question? existingQuestion = existingTopic.Questions.FirstOrDefault(q => q.Id == incomingQuestion.Id);
            if (existingQuestion is not null)
            {
                existingQuestion.QuestionNumber = incomingQuestion.QuestionNumber;
                existingQuestion.CostPositive = incomingQuestion.CostPositive;
                existingQuestion.CostNegative = incomingQuestion.CostNegative;
                existingQuestion.Text = incomingQuestion.Text;
                existingQuestion.Answer = incomingQuestion.Answer;
                existingQuestion.Comment = incomingQuestion.Comment;
            }
        }

        TopicAuthor? topicAuthor = existingTopic.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUserId);
        if (topicAuthor is not null)
        {
            topicAuthor.IsAuthor = isAuthor;
        }

        await _dbContext.SaveChangesAsync();

        return existingTopic;
    }

    public async Task<Topic?> DeleteAsync(Guid id)
    {
        Topic? existingTopic = await _dbContext.Topics.FirstOrDefaultAsync(x => x.Id == id);

        if (existingTopic is null)
            return null;

        _dbContext.Topics.Remove(existingTopic);
        await _dbContext.SaveChangesAsync();

        return existingTopic;
    }
}
