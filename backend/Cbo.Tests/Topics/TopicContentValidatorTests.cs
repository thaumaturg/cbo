using System.Collections.Generic;
using System.Linq;
using Cbo.Core.Domain;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Topics;

/// <summary>
/// The title, description and question rules that create, update and every topic of an import share. Each subclass
/// builds its own request from a <see cref="TopicData"/>, and <see cref="Prefix"/> is where that topic sits in it.
/// </summary>
public abstract class TopicContentValidatorTests<TRequest>
{
    protected sealed record QuestionData(
        int QuestionNumber, int Cost = 10, string Text = "Capital of France?", string Answer = "Paris", string? Comment = null);

    protected sealed record TopicData(
        string Title, string? Description, IReadOnlyList<QuestionData> Questions, IReadOnlyList<string> Authors);

    /// <summary>The path in front of the topic's own fields, such as <c>topics[0].</c>.</summary>
    protected abstract string Prefix { get; }

    protected abstract TestValidationResult<TRequest> Validate(TopicData topic);

    protected static TopicData ValidTopic => new TopicData("Geography", "Capitals of Europe", Questions(2), []);

    protected static IReadOnlyList<QuestionData> Questions(int count, int cost = 10) =>
        [.. Enumerable.Range(1, count).Select(number => new QuestionData(number, cost))];

    [Fact]
    public void A_complete_topic_is_valid()
    {
        Validate(ValidTopic).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_blank_title_is_required()
    {
        Validate(ValidTopic with { Title = "   " })
            .ShouldHaveValidationErrorFor(Prefix + "title").WithErrorMessage("The title is required.").Only();
    }

    // A JSON null for a required string reaches the validator as null
    [Fact]
    public void A_null_title_is_required()
    {
        Validate(ValidTopic with { Title = null! })
            .ShouldHaveValidationErrorFor(Prefix + "title").WithErrorMessage("The title is required.").Only();
    }

    [Fact]
    public void The_title_can_be_as_long_as_the_maximum()
    {
        Validate(ValidTopic with { Title = new string('t', Limits.Topics.TitleLengthMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_title_one_over_the_maximum_is_too_long()
    {
        Validate(ValidTopic with { Title = new string('t', Limits.Topics.TitleLengthMax + 1) })
            .ShouldHaveValidationErrorFor(Prefix + "title")
            .WithErrorMessage($"The title can be at most {Limits.Topics.TitleLengthMax} characters.")
            .Only();
    }

    [Fact]
    public void The_description_is_optional()
    {
        Validate(ValidTopic with { Description = null }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void The_description_can_be_as_long_as_the_maximum()
    {
        Validate(ValidTopic with { Description = new string('d', Limits.Topics.DescriptionLengthMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_description_one_over_the_maximum_is_too_long()
    {
        Validate(ValidTopic with { Description = new string('d', Limits.Topics.DescriptionLengthMax + 1) })
            .ShouldHaveValidationErrorFor(Prefix + "description")
            .WithErrorMessage($"The description can be at most {Limits.Topics.DescriptionLengthMax} characters.")
            .Only();
    }

    [Fact]
    public void A_topic_without_questions_has_too_few()
    {
        Validate(ValidTopic with { Questions = [] })
            .ShouldHaveValidationErrorFor(Prefix + "questions")
            .WithErrorMessage($"A topic must have at least {Limits.Topics.QuestionsMin} question(s).")
            .Only();
    }

    [Fact]
    public void A_topic_can_have_as_many_questions_as_the_maximum()
    {
        Validate(ValidTopic with { Questions = Questions(Limits.Topics.QuestionsMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_topic_with_one_question_over_the_maximum_has_too_many()
    {
        Validate(ValidTopic with { Questions = Questions(Limits.Topics.QuestionsMax + 1, cost: 1) })
            .ShouldHaveValidationErrorFor(Prefix + "questions")
            .WithErrorMessage($"A topic cannot have more than {Limits.Topics.QuestionsMax} questions.")
            .Only();
    }

    [Fact]
    public void Question_numbers_with_a_gap_are_not_sequential()
    {
        Validate(ValidTopic with { Questions = [new QuestionData(1), new QuestionData(3)] })
            .ShouldHaveValidationErrorFor(Prefix + "questions")
            .WithErrorMessage("Question numbers must be unique and sequential from 1 to 2.")
            .Only();
    }

    [Fact]
    public void A_repeated_question_number_is_not_unique()
    {
        Validate(ValidTopic with { Questions = [new QuestionData(1), new QuestionData(2), new QuestionData(2)] })
            .ShouldHaveValidationErrorFor(Prefix + "questions")
            .WithErrorMessage("Question numbers must be unique and sequential from 1 to 3.")
            .Only();
    }

    [Fact]
    public void Questions_in_any_order_are_sequential()
    {
        Validate(ValidTopic with { Questions = [new QuestionData(2), new QuestionData(1)] })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void The_total_cost_can_be_as_high_as_the_maximum()
    {
        Validate(ValidTopic with { Questions = Questions(2, cost: Limits.Topics.QuestionsCostTotalMax / 2) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_total_cost_one_over_the_maximum_is_too_high_and_names_the_total()
    {
        const int total = Limits.Topics.QuestionsCostTotalMax + 1;

        Validate(ValidTopic with { Questions = [new QuestionData(1, Cost: total - 1), new QuestionData(2, Cost: 1)] })
            .ShouldHaveValidationErrorFor(Prefix + "questions")
            .WithErrorMessage(
                $"The total cost of a topic's questions cannot be more than {Limits.Topics.QuestionsCostTotalMax} (currently {total}).")
            .Only();
    }

    [Fact]
    public void The_list_of_questions_stops_at_its_first_failure()
    {
        Validate(ValidTopic with { Questions = Questions(Limits.Topics.QuestionsMax + 1, cost: 100) })
            .ShouldHaveValidationErrorFor(Prefix + "questions")
            .WithErrorMessage($"A topic cannot have more than {Limits.Topics.QuestionsMax} questions.")
            .Only();
    }

    [Fact]
    public void Every_invalid_question_is_reported_at_its_index()
    {
        TestValidationResult<TRequest> result = Validate(ValidTopic with
        {
            Questions = [new QuestionData(1, Answer: ""), new QuestionData(2), new QuestionData(3, Text: "", Cost: 0)],
        });

        result.ShouldHaveValidationErrorFor(Prefix + "questions[0].answer").WithErrorMessage("Answer is required.");
        result.ShouldHaveValidationErrorFor(Prefix + "questions[2].text").WithErrorMessage("Question text is required.");
        result.ShouldHaveValidationErrorFor(Prefix + "questions[2].cost")
            .WithErrorMessage($"Cost must be between {Limits.Topics.QuestionCostMin} and {Limits.Topics.QuestionCostMax}.");
        Assert.Equal(3, result.Errors.Count);
    }
}
