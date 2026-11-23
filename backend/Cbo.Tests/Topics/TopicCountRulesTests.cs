using System.Collections.Generic;
using Cbo.Core.Topics;
using Cbo.Results;

namespace Cbo.Tests.Topics;

public class TopicCountRulesTests
{
    [Fact]
    public void Adding_a_topic_under_the_cap_is_allowed()
    {
        Assert.Null(TopicCountRules.CheckOwnedTopicsCap(ownedCount: 998, addedCount: 1));
    }

    [Fact]
    public void Reaching_exactly_the_cap_is_allowed()
    {
        Assert.Null(TopicCountRules.CheckOwnedTopicsCap(ownedCount: 999, addedCount: 1));
    }

    [Fact]
    public void Going_one_over_the_cap_is_a_conflict_naming_the_total()
    {
        Failure? failure = TopicCountRules.CheckOwnedTopicsCap(ownedCount: 1000, addedCount: 1);

        AssertCapConflict(failure, "You can own at most 1000 topics, and this would bring you to 1001.");
    }

    [Fact]
    public void An_import_that_would_go_over_the_cap_is_a_conflict_naming_the_total()
    {
        Failure? failure = TopicCountRules.CheckOwnedTopicsCap(ownedCount: 980, addedCount: 50);

        AssertCapConflict(failure, "You can own at most 1000 topics, and this would bring you to 1030.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void An_import_of_1_to_50_topics_is_allowed(int topicCount)
    {
        Assert.Empty(TopicCountRules.CheckImportSize(topicCount));
    }

    [Fact]
    public void An_import_with_no_topics_is_an_error_on_topics()
    {
        AssertTopicsError(TopicCountRules.CheckImportSize(0), "Choose at least 1 topic to import.");
    }

    [Fact]
    public void An_import_of_more_than_50_topics_is_an_error_on_topics()
    {
        AssertTopicsError(TopicCountRules.CheckImportSize(51), "You can import at most 50 topics at once.");
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
