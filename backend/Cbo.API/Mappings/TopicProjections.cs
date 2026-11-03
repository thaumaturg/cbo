using System.Linq.Expressions;
using Cbo.API.Contracts.Topics;
using Cbo.API.Models.Domain;

namespace Cbo.API.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class TopicProjections
{
    /// <summary>
    /// Questions come ordered by number, authors by id. <see cref="TopicResponse.IsAuthor"/> is the current user's flag.
    /// </summary>
    public static Expression<Func<Topic, TopicResponse>> ToResponse(Guid currentUserId)
    {
        Expression<Func<TopicAuthor, TopicAuthorResponse>> toAuthorResponse = TopicAuthorProjections.ToResponse();
        Expression<Func<Question, QuestionResponse>> toQuestionResponse = QuestionProjections.ToResponse();

        return t => new TopicResponse
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
                .Select(toQuestionResponse)
                .ToList(),
            Authors = t.TopicAuthors
                .AsQueryable()
                .OrderBy(ta => ta.Id)
                .Select(toAuthorResponse)
                .ToList(),
        };
    }
}
