using System.Collections.Generic;
using System.Linq;
using Cbo.Core.Validation;
using FluentValidation;
using FluentValidation.Results;

namespace Cbo.Tests.Validation;

/// <summary>
/// Runs with the options from <see cref="ValidationModuleInitializer"/>, the same ones the API uses.
/// </summary>
public class ValidationSetupTests
{
    private sealed class Question
    {
        public string Text { get; init; } = "";

        public int Cost { get; init; }
    }

    private sealed class Owner
    {
        public string FullName { get; init; } = "";
    }

    private sealed class Topic
    {
        public string Title { get; init; } = "";

        public Owner Owner { get; init; } = new Owner();

        public List<Question> Questions { get; init; } = [];

        public List<string> Tags { get; init; } = [];
    }

    private sealed class Import
    {
        public List<Topic> Topics { get; init; } = [];
    }

    private sealed class QuestionValidator : ChildValidator<Question>
    {
        public QuestionValidator()
        {
            RuleFor(q => q.Text).NotEmpty().WithMessage("Question text is required.");
            RuleFor(q => q.Cost).GreaterThan(0).WithMessage("Cost must be positive.");
        }
    }

    private sealed class TopicValidator : ChildValidator<Topic>
    {
        public TopicValidator()
        {
            RuleFor(t => t.Title)
                .NotEmpty().WithMessage("The title is required.")
                .MinimumLength(3).WithMessage("The title is too short.");
            RuleFor(t => t.Owner.FullName).NotEmpty().WithMessage("The owner's name is required.");
            RuleForEach(t => t.Questions).SetValidator(new QuestionValidator());
            RuleForEach(t => t.Tags).NotEmpty().WithMessage("A tag can't be empty.");
        }
    }

    private sealed class ImportValidator : RequestValidator<Import>
    {
        public ImportValidator()
        {
            RuleForEach(i => i.Topics).SetValidator(new TopicValidator());
        }

        public override string InvalidMessage => "Some of the topics are invalid.";
    }

    private static readonly Question s_validQuestion = new Question { Text = "Why?", Cost = 10 };

    [Fact]
    public void Nested_and_indexed_member_paths_become_camelCase()
    {
        var topic = new Topic
        {
            Title = "History",
            Questions = [s_validQuestion, new Question { Text = "", Cost = 10 }],
        };

        ValidationResult result = new TopicValidator().Validate(topic);

        Assert.Equal(["owner.fullName", "questions[1].text"], result.Errors.Select(e => e.PropertyName));
    }

    [Fact]
    public void Paths_through_two_collections_become_camelCase()
    {
        var import = new Import
        {
            Topics =
            [
                new Topic { Title = "History", Owner = new Owner { FullName = "Anna" }, Questions = [s_validQuestion] },
                new Topic { Title = "Science", Owner = new Owner { FullName = "Anna" }, Questions = [new Question { Text = "Why?", Cost = 0 }] },
            ],
        };

        ValidationResult result = new ImportValidator().Validate(import);

        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal("topics[1].questions[0].cost", failure.PropertyName);
        Assert.Equal("Cost must be positive.", failure.ErrorMessage);
    }

    [Fact]
    public void Checking_a_property_stops_at_its_first_failure_and_other_properties_are_still_checked()
    {
        var topic = new Topic { Title = "", Owner = new Owner { FullName = "" } };

        ValidationResult result = new TopicValidator().Validate(topic);

        Assert.Equal(
            [("title", "The title is required."), ("owner.fullName", "The owner's name is required.")],
            result.Errors.Select(e => (e.PropertyName, e.ErrorMessage)));
    }

    [Fact]
    public void Every_invalid_item_in_nested_lists_is_reported_and_each_item_property_stops_at_its_first_failure()
    {
        var import = new Import
        {
            Topics =
            [
                new Topic { Title = "", Owner = new Owner { FullName = "Anna" }, Tags = ["", "war", ""] },
                new Topic { Title = "Science", Owner = new Owner { FullName = "Anna" }, Questions = [s_validQuestion] },
                new Topic
                {
                    Title = "Art",
                    Owner = new Owner { FullName = "Anna" },
                    Questions = [new Question { Text = "", Cost = 0 }, s_validQuestion, new Question { Text = "Who?", Cost = 0 }],
                },
            ],
        };

        ValidationResult result = new ImportValidator().Validate(import);

        Assert.Equal(
            [
                ("topics[0].title", "The title is required."),
                ("topics[0].tags[0]", "A tag can't be empty."),
                ("topics[0].tags[2]", "A tag can't be empty."),
                ("topics[2].questions[0].text", "Question text is required."),
                ("topics[2].questions[0].cost", "Cost must be positive."),
                ("topics[2].questions[2].cost", "Cost must be positive."),
            ],
            result.Errors.Select(e => (e.PropertyName, e.ErrorMessage)));
    }

    [Fact]
    public void Default_messages_name_the_property_in_camelCase()
    {
        var validator = new InlineValidator<Topic>();
        validator.RuleFor(t => t.Title).NotEmpty();

        ValidationFailure failure = Assert.Single(validator.Validate(new Topic()).Errors);

        Assert.Equal("'title' must not be empty.", failure.ErrorMessage);
    }
}
