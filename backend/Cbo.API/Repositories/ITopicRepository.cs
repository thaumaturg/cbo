using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITopicRepository
{
    Task<List<Topic>> GetAllAsync();
    Task<List<Topic>> GetAllByUserIdAsync(int userId);
    Task<Topic?> GetByIdAsync(int id);
    Task<Topic?> GetByIdIncludeQuestionsAsync(int id);
    Task<Topic> CreateAsync(Topic topic);
    Task<Topic?> UpdateAsync(int id, Topic updatedTopic);
    Task<Topic?> DeleteAsync(int id);
}
