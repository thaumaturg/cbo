using System.Linq.Expressions;
using Cbo.Core.Domain;
using Cbo.Core.Tournaments;

namespace Cbo.Core.Topics;

/// <inheritdoc cref="TournamentProjections"/>
public static class TopicProjections
{
    /// <summary>
    /// Questions come ordered by number. Authors come with the owner first, then by username, case-insensitively.
    /// <see cref="TopicResponse.IsAuthor"/> is the current user's flag.
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
                .OrderByDescending(ta => ta.IsOwner)
                .ThenBy(ta => ta.ApplicationUser!.NormalizedUserName)
                .Select(toAuthorResponse)
                .ToList(),
        };
    }
}
