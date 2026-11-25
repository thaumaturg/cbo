using System;
using System.Collections.Generic;
using System.Linq;
using Cbo.Core.Domain;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Rounds;

/// <summary>
/// The rules that create and update round requests share. Each subclass builds its own request from a
/// <see cref="RoundData"/>.
/// </summary>
public abstract class RoundRequestValidatorTests<TRequest>
{
    protected sealed record AnswerData(
        Guid QuestionId,
        Guid MatchParticipantId,
        bool? IsAnswerAccepted = null,
        int? OverrideCost = null);

    protected sealed record RoundData(int NumberInMatch, bool IsOverrideMode, IReadOnlyList<AnswerData> Answers);

    protected abstract string InvalidMessage { get; }

    protected abstract TestValidationResult<TRequest> Validate(RoundData round);

    private static readonly string RoundNumberOutOfRange =
        $"Round number must be between 1 and {Limits.Matches.RoundsPerQualificationMatch}.";

    private static readonly Guid Question1 = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Question2 = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid Participant1 = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid Participant2 = Guid.Parse("00000000-0000-0000-0000-0000000000b2");

    private static RoundData StandardRound(params AnswerData[] answers) => new(1, IsOverrideMode: false, answers);

    private static RoundData StandardRound() =>
        StandardRound(new AnswerData(Question1, Participant1, true), new AnswerData(Question1, Participant2, false));

    private static RoundData OverrideRound(params AnswerData[] answers) => new(1, IsOverrideMode: true, answers);

    private static RoundData Round(bool isOverrideMode, params AnswerData[] answers) => new(1, isOverrideMode, answers);

    // A valid, unaccepted answer for the mode
    private static AnswerData Answer(bool isOverrideMode, Guid questionId, Guid matchParticipantId) =>
        isOverrideMode
            ? new AnswerData(questionId, matchParticipantId, OverrideCost: 10)
            : new AnswerData(questionId, matchParticipantId, IsAnswerAccepted: false);

    [Fact]
    public void Invalid_message_is_about_the_round()
    {
        Assert.Equal("Some of the round data is invalid.", InvalidMessage);
    }

    [Fact]
    public void The_round_number_can_be_1()
    {
        Validate(StandardRound() with { NumberInMatch = 1 }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void The_round_number_can_be_the_last_round_of_a_match()
    {
        Validate(StandardRound() with { NumberInMatch = Limits.Matches.RoundsPerQualificationMatch })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Round_number_0_is_out_of_range()
    {
        Validate(StandardRound() with { NumberInMatch = 0 })
            .ShouldHaveValidationErrorFor("numberInMatch").WithErrorMessage(RoundNumberOutOfRange).Only();
    }

    [Fact]
    public void A_round_number_one_over_the_last_round_is_out_of_range()
    {
        Validate(StandardRound() with { NumberInMatch = Limits.Matches.RoundsPerQualificationMatch + 1 })
            .ShouldHaveValidationErrorFor("numberInMatch").WithErrorMessage(RoundNumberOutOfRange).Only();
    }

    [Fact]
    public void A_standard_round_with_every_answer_marked_is_valid()
    {
        Validate(StandardRound()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_round_without_answers_is_valid()
    {
        Validate(StandardRound([])).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void In_standard_mode_an_unmarked_answer_is_invalid()
    {
        Validate(StandardRound(new AnswerData(Question1, Participant1, true), new AnswerData(Question1, Participant2)))
            .ShouldHaveValidationErrorFor("answers[1].isAnswerAccepted")
            .WithErrorMessage("In standard mode, IsAnswerAccepted must be provided for all answers.")
            .Only();
    }

    [Fact]
    public void In_standard_mode_an_override_cost_is_invalid()
    {
        Validate(StandardRound(new AnswerData(Question1, Participant1, false, OverrideCost: 10)))
            .ShouldHaveValidationErrorFor("answers[0].overrideCost")
            .WithErrorMessage("In standard mode, OverrideCost must be null for all answers.")
            .Only();
    }

    [Fact]
    public void In_standard_mode_every_wrong_answer_is_reported()
    {
        TestValidationResult<TRequest> result = Validate(StandardRound(
            new AnswerData(Question1, Participant1, OverrideCost: 10),
            new AnswerData(Question1, Participant2)));

        result.ShouldHaveValidationErrorFor("answers[0].isAnswerAccepted")
            .WithErrorMessage("In standard mode, IsAnswerAccepted must be provided for all answers.");
        result.ShouldHaveValidationErrorFor("answers[0].overrideCost")
            .WithErrorMessage("In standard mode, OverrideCost must be null for all answers.");
        result.ShouldHaveValidationErrorFor("answers[1].isAnswerAccepted")
            .WithErrorMessage("In standard mode, IsAnswerAccepted must be provided for all answers.");
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void An_override_round_with_a_cost_for_every_answer_is_valid()
    {
        Validate(OverrideRound(new AnswerData(Question1, Participant1, OverrideCost: 30), new AnswerData(Question1, Participant2, OverrideCost: -30)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void In_override_mode_a_marked_answer_is_invalid()
    {
        Validate(OverrideRound(new AnswerData(Question1, Participant1, IsAnswerAccepted: true, OverrideCost: 30)))
            .ShouldHaveValidationErrorFor("answers[0].isAnswerAccepted")
            .WithErrorMessage("In override mode, IsAnswerAccepted must be null for all answers.")
            .Only();
    }

    [Fact]
    public void In_override_mode_an_answer_without_a_cost_is_invalid()
    {
        Validate(OverrideRound(new AnswerData(Question1, Participant1, OverrideCost: 30), new AnswerData(Question1, Participant2)))
            .ShouldHaveValidationErrorFor("answers[1].overrideCost")
            .WithErrorMessage("In override mode, OverrideCost must be provided for all answers.")
            .Only();
    }

    [Theory]
    [InlineData(Limits.Rounds.OverrideCostMin)]
    [InlineData(0)]
    [InlineData(Limits.Rounds.OverrideCostMax)]
    public void The_override_cost_can_be_anywhere_in_its_range(int overrideCost)
    {
        Validate(OverrideRound(new AnswerData(Question1, Participant1, OverrideCost: overrideCost)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(Limits.Rounds.OverrideCostMin - 1)]
    [InlineData(Limits.Rounds.OverrideCostMax + 1)]
    public void An_override_cost_one_past_its_range_is_out_of_range(int overrideCost)
    {
        Validate(OverrideRound(new AnswerData(Question1, Participant1, OverrideCost: overrideCost)))
            .ShouldHaveValidationErrorFor("answers[0].overrideCost")
            .WithErrorMessage(
                $"The override cost must be between {Limits.Rounds.OverrideCostMin} and {Limits.Rounds.OverrideCostMax}.")
            .Only();
    }

    [Fact]
    public void In_override_mode_every_wrong_answer_is_reported()
    {
        TestValidationResult<TRequest> result = Validate(OverrideRound(
            new AnswerData(Question1, Participant1, IsAnswerAccepted: false),
            new AnswerData(Question1, Participant2, IsAnswerAccepted: true, OverrideCost: 30)));

        result.ShouldHaveValidationErrorFor("answers[0].isAnswerAccepted")
            .WithErrorMessage("In override mode, IsAnswerAccepted must be null for all answers.");
        result.ShouldHaveValidationErrorFor("answers[0].overrideCost")
            .WithErrorMessage("In override mode, OverrideCost must be provided for all answers.");
        result.ShouldHaveValidationErrorFor("answers[1].isAnswerAccepted")
            .WithErrorMessage("In override mode, IsAnswerAccepted must be null for all answers.");
        Assert.Equal(3, result.Errors.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Each_participant_can_answer_each_question_once(bool isOverrideMode)
    {
        Validate(Round(isOverrideMode,
                Answer(isOverrideMode, Question1, Participant1),
                Answer(isOverrideMode, Question1, Participant2),
                Answer(isOverrideMode, Question2, Participant1),
                Answer(isOverrideMode, Question2, Participant2)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_second_answer_by_a_participant_to_a_question_is_reported_on_that_answer(bool isOverrideMode)
    {
        Validate(Round(isOverrideMode,
                Answer(isOverrideMode, Question1, Participant1),
                Answer(isOverrideMode, Question1, Participant2),
                Answer(isOverrideMode, Question1, Participant1)))
            .ShouldHaveValidationErrorFor("answers[2]")
            .WithErrorMessage($"Participant {Participant1} has more than one answer to question {Question1}.")
            .Only();
    }

    [Fact]
    public void Every_repeated_answer_is_reported()
    {
        TestValidationResult<TRequest> result = Validate(StandardRound(
            new AnswerData(Question1, Participant1, false),
            new AnswerData(Question1, Participant1, false),
            new AnswerData(Question2, Participant2, false),
            new AnswerData(Question2, Participant2, false),
            new AnswerData(Question1, Participant1, false)));

        result.ShouldHaveValidationErrorFor("answers[1]")
            .WithErrorMessage($"Participant {Participant1} has more than one answer to question {Question1}.");
        result.ShouldHaveValidationErrorFor("answers[3]")
            .WithErrorMessage($"Participant {Participant2} has more than one answer to question {Question2}.");
        result.ShouldHaveValidationErrorFor("answers[4]")
            .WithErrorMessage($"Participant {Participant1} has more than one answer to question {Question1}.");
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void In_standard_mode_one_participant_can_answer_a_question_correctly()
    {
        Validate(StandardRound(
                new AnswerData(Question1, Participant1, true),
                new AnswerData(Question1, Participant2, false),
                new AnswerData(Question2, Participant2, true)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void In_standard_mode_two_correct_answers_to_one_question_are_invalid()
    {
        Validate(StandardRound(new AnswerData(Question1, Participant1, true), new AnswerData(Question1, Participant2, true)))
            .ShouldHaveValidationErrorFor("answers")
            .WithErrorMessage(
                $"Question {Question1} has multiple correct answers. Only one correct answer is allowed per question.")
            .Only();
    }

    [Fact]
    public void Every_question_with_several_correct_answers_is_reported()
    {
        TestValidationResult<TRequest> result = Validate(StandardRound(
            new AnswerData(Question1, Participant1, true),
            new AnswerData(Question2, Participant1, true),
            new AnswerData(Question1, Participant2, true),
            new AnswerData(Question2, Participant2, true)));

        ITestValidationWith errors = result.ShouldHaveValidationErrorFor("answers");
        Assert.Equal(
            [
                $"Question {Question1} has multiple correct answers. Only one correct answer is allowed per question.",
                $"Question {Question2} has multiple correct answers. Only one correct answer is allowed per question.",
            ],
            errors.Select(e => e.ErrorMessage));
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Several_overrides_with_a_positive_cost_for_one_question_are_valid()
    {
        Validate(OverrideRound(
                new AnswerData(Question1, Participant1, OverrideCost: 30),
                new AnswerData(Question1, Participant2, OverrideCost: 30)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Errors_in_the_round_number_and_the_answers_are_all_reported()
    {
        TestValidationResult<TRequest> result = Validate(StandardRound(
            new AnswerData(Question1, Participant1, true),
            new AnswerData(Question1, Participant2, true),
            new AnswerData(Question1, Participant2)) with { NumberInMatch = 0 });

        result.ShouldHaveValidationErrorFor("numberInMatch").WithErrorMessage(RoundNumberOutOfRange);
        result.ShouldHaveValidationErrorFor("answers[2].isAnswerAccepted")
            .WithErrorMessage("In standard mode, IsAnswerAccepted must be provided for all answers.");
        result.ShouldHaveValidationErrorFor("answers[2]")
            .WithErrorMessage($"Participant {Participant2} has more than one answer to question {Question1}.");
        result.ShouldHaveValidationErrorFor("answers")
            .WithErrorMessage(
                $"Question {Question1} has multiple correct answers. Only one correct answer is allowed per question.");
        Assert.Equal(4, result.Errors.Count);
    }
}
