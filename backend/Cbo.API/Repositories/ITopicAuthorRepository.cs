using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITopicAuthorRepository
{
    Task<List<TopicAuthor>> GetAllByTopicIdAsync(int topicId);
    Task<TopicAuthor?> GetByIdAsync(int id);
    Task<TopicAuthor?> GetByAuthorIdAndTopicIdAsync(int authorId, int topicId);
    Task<TopicAuthor?> GetByUserIdAndTopicIdAsync(int userId, int topicId);
    Task<TopicAuthor> CreateAsync(TopicAuthor topicAuthor);
    Task<TopicAuthor?> DeleteAsync(int id);
}
