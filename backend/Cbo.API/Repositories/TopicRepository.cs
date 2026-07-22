using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public interface ITopicRepository
{
    Task<List<Topic>> GetAllByUserIdAsync(Guid userId);
    Task<Topic?> GetByIdAsync(Guid id);
    Task<Topic?> GetByIdIncludeQuestionsAsync(Guid id);
    Task<Topic> CreateAsync(Topic topic);
    Task<Topic?> UpdateAsync(Guid id, UpdateTopicParameters parameters, Guid currentUserId);
    Task<Topic?> DeleteAsync(Guid id);
}

public class TopicRepository(CboDbContext dbContext) : ITopicRepository
{
    private readonly CboDbContext _dbContext = dbContext;

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

    public async Task<Topic?> UpdateAsync(Guid id, UpdateTopicParameters parameters, Guid currentUserId)
    {
        Topic? existingTopic = await _dbContext.Topics
            .Include(t => t.Questions)
            .Include(t => t.TopicAuthors)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existingTopic is null)
            return null;

        HashSet<Guid> existingQuestionIds = existingTopic.Questions.Select(q => q.Id).ToHashSet();
        HashSet<Guid> incomingQuestionIds = parameters.Questions
            .Where(q => q.Id.HasValue)
            .Select(q => q.Id!.Value)
            .ToHashSet();

        List<Guid> unknownIds = incomingQuestionIds.Except(existingQuestionIds).ToList();
        if (unknownIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Questions with IDs {string.Join(", ", unknownIds)} do not belong to this topic.");
        }

        existingTopic.Title = parameters.Title;
        existingTopic.Description = parameters.Description;

        // Sync questions: delete absent, update matched, insert new (Id == null)
        List<Question> questionsToRemove = existingTopic.Questions
            .Where(q => !incomingQuestionIds.Contains(q.Id))
            .ToList();
        _dbContext.Questions.RemoveRange(questionsToRemove);

        foreach (UpdateQuestionParameters questionParam in parameters.Questions)
        {
            if (questionParam.Id.HasValue)
            {
                Question existingQuestion = existingTopic.Questions.First(q => q.Id == questionParam.Id.Value);
                existingQuestion.QuestionNumber = questionParam.QuestionNumber;
                existingQuestion.CostPositive = questionParam.CostPositive;
                existingQuestion.CostNegative = questionParam.CostNegative;
                existingQuestion.Text = questionParam.Text;
                existingQuestion.Answer = questionParam.Answer;
                existingQuestion.Comment = questionParam.Comment;
            }
            else
            {
                existingTopic.Questions.Add(new Question
                {
                    QuestionNumber = questionParam.QuestionNumber,
                    CostPositive = questionParam.CostPositive,
                    CostNegative = questionParam.CostNegative,
                    Text = questionParam.Text,
                    Answer = questionParam.Answer,
                    Comment = questionParam.Comment,
                    TopicId = existingTopic.Id
                });
            }
        }

        TopicAuthor? topicAuthor = existingTopic.TopicAuthors.FirstOrDefault(ta => ta.ApplicationUserId == currentUserId);
        if (topicAuthor is not null)
        {
            topicAuthor.IsAuthor = parameters.IsAuthor;
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
