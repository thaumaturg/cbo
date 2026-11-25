using System;
using System.Collections.Generic;
using Cbo.Core.TournamentTopics;
using Cbo.Tests.Validation;
using FluentValidation.TestHelper;

namespace Cbo.Tests.TournamentTopics;

public class TournamentTopicListValidatorTests
{
    private static readonly Guid TopicA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid TopicB = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid TopicC = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    private readonly TournamentTopicListValidator _validator = new();

    [Fact]
    public void Invalid_message_is_about_the_tournament_topics()
    {
        Assert.Equal("Some of the tournament topics are invalid.", _validator.InvalidMessage);
    }

    // Empty slots between topics are fine, and the upper bound is the tournament's own maximum, checked by the handler
    [Fact]
    public void Different_topics_at_different_positions_are_valid()
    {
        _validator.TestValidate(Topics((TopicA, 0), (TopicB, 3), (TopicC, 1000))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_empty_list_is_valid()
    {
        _validator.TestValidate(Topics()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_negative_priority_position_is_an_error()
    {
        _validator.TestValidate(Topics((TopicA, 0), (TopicB, -1)))
            .ShouldHaveValidationErrorFor("[1].priorityIndex")
            .WithErrorMessage("The priority position must be 1 or greater.")
            .Only();
    }

    [Fact]
    public void A_repeated_priority_position_is_an_error_on_each_later_topic()
    {
        TestValidationResult<List<TournamentTopicRequest>> result =
            _validator.TestValidate(Topics((TopicA, 2), (TopicB, 2), (TopicC, 2)));

        Assert.Collection(
            result.Errors,
            error => AssertError("[1].priorityIndex", "This priority position appears more than once.", error),
            error => AssertError("[2].priorityIndex", "This priority position appears more than once.", error));
    }

    [Fact]
    public void A_negative_position_is_not_also_reported_as_repeated()
    {
        TestValidationResult<List<TournamentTopicRequest>> result =
            _validator.TestValidate(Topics((TopicA, -1), (TopicB, -1)));

        Assert.Collection(
            result.Errors,
            error => AssertError("[0].priorityIndex", "The priority position must be 1 or greater.", error),
            error => AssertError("[1].priorityIndex", "The priority position must be 1 or greater.", error));
    }

    [Fact]
    public void A_repeated_topic_is_an_error_on_each_later_occurrence()
    {
        TestValidationResult<List<TournamentTopicRequest>> result =
            _validator.TestValidate(Topics((TopicA, 0), (TopicA, 1), (TopicA, 2)));

        Assert.Collection(
            result.Errors,
            error => AssertError("[1].topicId", "This topic appears more than once.", error),
            error => AssertError("[2].topicId", "This topic appears more than once.", error));
    }

    [Fact]
    public void Errors_of_every_topic_are_reported_in_order()
    {
        TestValidationResult<List<TournamentTopicRequest>> result =
            _validator.TestValidate(Topics((TopicA, 0), (TopicA, 0), (TopicB, -5)));

        Assert.Collection(
            result.Errors,
            error => AssertError("[1].priorityIndex", "This priority position appears more than once.", error),
            error => AssertError("[1].topicId", "This topic appears more than once.", error),
            error => AssertError("[2].priorityIndex", "The priority position must be 1 or greater.", error));
    }

    [Fact]
    public void A_missing_topic_is_an_error_and_the_others_are_still_checked()
    {
        List<TournamentTopicRequest> topics = Topics((TopicA, 0), (TopicB, -1));
        topics.Insert(1, null!);

        TestValidationResult<List<TournamentTopicRequest>> result = _validator.TestValidate(topics);

        Assert.Collection(
            result.Errors,
            error => AssertError("[1]", "This topic is missing.", error),
            error => AssertError("[2].priorityIndex", "The priority position must be 1 or greater.", error));
    }

    // The API's JSON settings reject a null property but not a null list item, so the validator has to
    [Fact]
    public void A_null_in_the_body_is_a_missing_topic()
    {
        List<TournamentTopicRequest> topics = ApiJson.Read<List<TournamentTopicRequest>>("[null]");

        _validator.TestValidate(topics)
            .ShouldHaveValidationErrorFor("[0]")
            .WithErrorMessage("This topic is missing.")
            .Only();
    }

    private static List<TournamentTopicRequest> Topics(params (Guid TopicId, int PriorityIndex)[] topics) =>
        [.. Array.ConvertAll(topics, t => new TournamentTopicRequest { TopicId = t.TopicId, PriorityIndex = t.PriorityIndex })];

    private static void AssertError(string field, string message, FluentValidation.Results.ValidationFailure error)
    {
        Assert.Equal(field, error.PropertyName);
        Assert.Equal(message, error.ErrorMessage);
    }
}
