using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Core.Topics;

/// <summary>
/// Limits on how many topics an account owns and imports at once. Pure, so the handler counts the owned topics first.
/// </summary>
public static class TopicCountRules
{
    private const string TopicsField = "topics";

    /// <summary>
    /// Returns a conflict when owning <paramref name="addedCount"/> more topics would take the account past
    /// <see cref="Limits.Topics.OwnedMax"/>, or null when it wouldn't.
    /// </summary>
    public static Failure? CheckOwnedTopicsCap(int ownedCount, int addedCount)
    {
        int total = ownedCount + addedCount;

        return total > Limits.Topics.OwnedMax ? TopicFailures.OwnedTopicsCapReached(total) : null;
    }

    /// <summary>
    /// Returns an error on <c>topics</c> unless an import holds from 1 to
    /// <see cref="Limits.Topics.ImportMax"/> topics, otherwise none.
    /// </summary>
    public static IReadOnlyList<FieldError> CheckImportSize(int topicCount)
    {
        if (topicCount < 1)
            return [new FieldError(TopicsField, "Choose at least 1 topic to import.")];

        if (topicCount > Limits.Topics.ImportMax)
        {
            return [new FieldError(
                TopicsField, $"You can import at most {Limits.Topics.ImportMax} topics at once.")];
        }

        return [];
    }
}
