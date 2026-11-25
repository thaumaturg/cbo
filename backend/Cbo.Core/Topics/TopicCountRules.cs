using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Core.Topics;

/// <summary>
/// Limits on how many topics an account owns. Pure, so the handler counts the owned topics first.
/// </summary>
public static class TopicCountRules
{
    /// <summary>
    /// Returns a conflict when owning <paramref name="addedCount"/> more topics would take the account past
    /// <see cref="Limits.Topics.OwnedMax"/>, or null when it wouldn't.
    /// </summary>
    public static Failure? CheckOwnedTopicsCap(int ownedCount, int addedCount)
    {
        int total = ownedCount + addedCount;

        return total > Limits.Topics.OwnedMax ? TopicFailures.OwnedTopicsCapReached(total) : null;
    }
}
