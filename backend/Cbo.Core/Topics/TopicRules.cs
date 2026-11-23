using Cbo.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Topics;

/// <summary>
/// Rules shared by the topic command handlers.
/// </summary>
internal static class TopicRules
{
    /// <summary>
    /// A topic is played once a round uses it. Its questions, authors and existence are then fixed.
    /// </summary>
    public static Task<bool> IsPlayedAsync(CboDbContext dbContext, Guid topicId, CancellationToken cancellationToken) =>
        dbContext.Rounds.AnyAsync(r => r.TopicId == topicId, cancellationToken);

    /// <summary>
    /// Counts the topics the account owns, the ones it manages rather than only authors.
    /// </summary>
    public static Task<int> CountOwnedAsync(CboDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        dbContext.TopicAuthors.CountAsync(ta => ta.ApplicationUserId == userId && ta.IsOwner, cancellationToken);
}
