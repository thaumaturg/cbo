using Cbo.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Topics;

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
}
