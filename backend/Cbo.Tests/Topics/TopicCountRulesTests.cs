using System.Collections.Generic;
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

    [Theory]
    [InlineData(1)]
    [InlineData(Limits.Topics.ImportMax)]
    public void An_import_of_1_up_to_the_import_max_is_allowed(int topicCount)
    {
        Assert.Empty(TopicCountRules.CheckImportSize(topicCount));
    }

    [Fact]
    public void An_import_with_no_topics_is_an_error_on_topics()
    {
        AssertTopicsError(TopicCountRules.CheckImportSize(0), "Choose at least 1 topic to import.");
    }

    [Fact]
    public void An_import_over_the_import_max_is_an_error_on_topics()
    {
        AssertTopicsError(TopicCountRules.CheckImportSize(Limits.Topics.ImportMax + 1),
            $"You can import at most {Limits.Topics.ImportMax} topics at once.");
    }

    private static void AssertTopicsError(IReadOnlyList<FieldError> errors, string message)
    {
        FieldError error = Assert.Single(errors);
        Assert.Equal("topics", error.Field);
        Assert.Equal(message, error.Message);
    }

    private static void AssertCapConflict(Failure? failure, string message)
    {
        Assert.NotNull(failure);
        Assert.Equal(FailureKind.Conflict, failure.Kind);
        Assert.Equal(message, failure.Message);
        Assert.Empty(failure.FieldErrors);
    }
}
