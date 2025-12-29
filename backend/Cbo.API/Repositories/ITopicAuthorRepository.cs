using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITopicAuthorRepository
{
    Task<List<TopicAuthor>> GetAllByTopicIdAsync(Guid topicId);
    Task<TopicAuthor?> GetByAuthorIdAndTopicIdAsync(Guid authorId, Guid topicId);
    Task<TopicAuthor?> GetByUserIdAndTopicIdAsync(Guid userId, Guid topicId);
    Task<TopicAuthor> CreateAsync(TopicAuthor topicAuthor);
    Task<TopicAuthor?> DeleteAsync(Guid id);
}
