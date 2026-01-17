using Cbo.API.Models.Domain;

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
