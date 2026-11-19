using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Topics;

/// <summary>The filters and sort keys of the topics list (ADR-0008). Every filter must translate to SQL.</summary>
public static class TopicListFilter
{
    public const string DefaultSort = "-createdAt";

    // For the OpenAPI document: keep the keys in step with SortKeys
    public const string SortDescription =
        "One sort key, with an optional leading - for descending. Allowed: title, questionCount, createdAt.";

    public static readonly SortKeys<Topic> SortKeys = new SortKeys<Topic>(DefaultSort, t => t.Id)
        .Add("title", t => t.Title)
        .Add("questionCount", t => t.Questions.Count)
        .Add("createdAt", t => t.CreatedAt);

    /// <summary>Limits the topics to those the current user owns, then applies the query's filters.</summary>
    public static IQueryable<Topic> Apply(IQueryable<Topic> topics, GetTopicsQuery query, Guid currentUserId)
    {
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Topic> filtered = topics.Where(t => t.TopicAuthors.Any(ta => ta.ApplicationUserId == currentUserId && ta.IsOwner));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = LikePattern.Contains(query.Search);
            filtered = filtered.Where(t => EF.Functions.ILike(t.Title, pattern, LikePattern.EscapeCharacter));
        }

        filtered = query.Played switch
        {
            true => filtered.Where(t => t.Rounds.Any()),
            false => filtered.Where(t => !t.Rounds.Any()),
            null => filtered,
        };

        filtered = query.IsAuthor switch
        {
            true => filtered.Where(t => t.TopicAuthors.Any(ta => ta.ApplicationUserId == currentUserId && ta.IsAuthor)),
            false => filtered.Where(t => !t.TopicAuthors.Any(ta => ta.ApplicationUserId == currentUserId && ta.IsAuthor)),
            null => filtered,
        };

        return filtered;
    }
}
