using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Repositories;

public interface ITopicRepository
{
    Task<Topic?> GetByIdIncludeQuestionsAsync(Guid id);
}

public class TopicRepository(CboDbContext dbContext) : ITopicRepository
{
    private readonly CboDbContext _dbContext = dbContext;

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
}
