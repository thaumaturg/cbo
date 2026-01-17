using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public static class TopicMappings
{
    /// <summary>
    /// Maps a Topic to GetTopicDto.
    /// Requires: Questions, TopicAuthors collections to be loaded.
    /// </summary>
    public static GetTopicDto ToGetDto(this Topic topic, bool isPlayed, bool isAuthor)
    {
        return new GetTopicDto
        {
            Id = topic.Id,
            Title = topic.Title,
            Description = topic.Description,
            IsPlayed = isPlayed,
            IsAuthor = isAuthor,
            Questions = topic.Questions.OrderBy(q => q.QuestionNumber).Select(q => q.ToGetDto()).ToList(),
            Authors = topic.TopicAuthors.Select(a => a.ToGetDto()).ToList()
        };
    }

    public static Topic ToDomain(this CreateTopicDto dto)
    {
        return new Topic
        {
            Title = dto.Title,
            Description = dto.Description
        };
    }

    public static Question ToDomain(this CreateQuestionDto dto, Guid topicId)
    {
        return new Question
        {
            QuestionNumber = dto.QuestionNumber,
            CostPositive = dto.CostPositive,
            CostNegative = dto.CostNegative,
            Text = dto.Text,
            Answer = dto.Answer,
            Comment = dto.Comment,
            TopicId = topicId
        };
    }

    public static GetQuestionDto ToGetDto(this Question question)
    {
        return new GetQuestionDto
        {
            Id = question.Id,
            QuestionNumber = question.QuestionNumber,
            CostPositive = question.CostPositive,
            CostNegative = question.CostNegative,
            Text = question.Text,
            Answer = question.Answer,
            Comment = question.Comment,
            TopicId = question.TopicId
        };
    }

    public static GetTopicAuthorDto ToGetDto(this TopicAuthor author)
    {
        return new GetTopicAuthorDto
        {
            Id = author.Id,
            Username = author.ApplicationUser?.UserName ?? string.Empty,
            IsOwner = author.IsOwner,
            IsAuthor = author.IsAuthor,
            TopicId = author.TopicId,
            ApplicationUserId = author.ApplicationUserId
        };
    }
}
