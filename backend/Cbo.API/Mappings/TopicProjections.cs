using System.Linq.Expressions;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class TopicProjections
{
    /// <summary>
    /// Questions come ordered by number, authors by id. <see cref="GetTopicDto.IsAuthor"/> is the current user's flag.
    /// </summary>
    public static Expression<Func<Topic, GetTopicDto>> ToGetDto(Guid currentUserId)
    {
        Expression<Func<TopicAuthor, GetTopicAuthorDto>> toAuthorDto = TopicAuthorProjections.ToGetDto();
        Expression<Func<Question, GetQuestionDto>> toQuestionDto = QuestionProjections.ToGetDto();

        return t => new GetTopicDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            IsPlayed = t.Rounds.Any(),
            IsAuthor = t.TopicAuthors
                .Where(ta => ta.ApplicationUserId == currentUserId)
                .Select(ta => ta.IsAuthor)
                .FirstOrDefault(),
            Questions = t.Questions
                .AsQueryable()
                .OrderBy(q => q.QuestionNumber)
                .Select(toQuestionDto)
                .ToList(),
            Authors = t.TopicAuthors
                .AsQueryable()
                .OrderBy(ta => ta.Id)
                .Select(toAuthorDto)
                .ToList(),
        };
    }
}
