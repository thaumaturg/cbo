using Cbo.Core.Domain;
using Cbo.Core.Topics;
using Cbo.Results;

namespace Cbo.Tests.Topics;

public class TopicCountRulesTests
{
    [Fact]
    public void Adding_a_topic_under_the_cap_is_allowed()
    {
        Assert.Null(TopicCountRules.CheckOwnedTopicsCap(ownedCount: Limits.Topics.OwnedMax - 2, addedCount: 1));
    }

    [Fact]
    public void Reaching_exactly_the_cap_is_allowed()
    {
        Assert.Null(TopicCountRules.CheckOwnedTopicsCap(ownedCount: Limits.Topics.OwnedMax - 1, addedCount: 1));
    }

    [Fact]
    public void Going_one_over_the_cap_is_a_conflict_naming_the_total()
    {
        Failure? failure = TopicCountRules.CheckOwnedTopicsCap(ownedCount: Limits.Topics.OwnedMax, addedCount: 1);

        AssertCapConflict(failure,
            $"You can own at most {Limits.Topics.OwnedMax} topics, and this would bring you to {Limits.Topics.OwnedMax + 1}.");
    }

    [Fact]
    public void An_import_that_would_go_over_the_cap_is_a_conflict_naming_the_total()
    {
        const int ownedCount = Limits.Topics.OwnedMax - 20;
        Failure? failure = TopicCountRules.CheckOwnedTopicsCap(ownedCount, addedCount: Limits.Topics.ImportMax);

        AssertCapConflict(failure,
            $"You can own at most {Limits.Topics.OwnedMax} topics, and this would bring you to {ownedCount + Limits.Topics.ImportMax}.");
    }

    private static void AssertCapConflict(Failure? failure, string message)
    {
        Assert.NotNull(failure);
        Assert.Equal(FailureKind.Conflict, failure.Kind);
        Assert.Equal(message, failure.Message);
        Assert.Empty(failure.FieldErrors);
    }
}
