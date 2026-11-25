using System.Linq;
using Cbo.Core.Domain;
using Cbo.Core.Topics;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Topics;

/// <summary>
/// The import's own size rules, and the shared topic rules checked on its only topic at <c>topics[0]</c>.
/// </summary>
public class ImportTopicsRequestValidatorTests : TopicContentValidatorTests<ImportTopicsRequest>
{
    private readonly ImportTopicsRequestValidator _validator = new ImportTopicsRequestValidator();

    protected override string Prefix => "topics[0].";

    [Fact]
    public void Invalid_message_is_about_the_topics()
    {
        Assert.Equal("Some of the topics are invalid.", _validator.InvalidMessage);
    }

    [Fact]
    public void An_import_can_have_as_many_topics_as_the_maximum()
    {
        _validator.TestValidate(Import(Limits.Topics.ImportMax)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_import_without_topics_has_too_few()
    {
        _validator.TestValidate(Import(0))
            .ShouldHaveValidationErrorFor("topics").WithErrorMessage("Choose at least 1 topic to import.").Only();
    }

    [Fact]
    public void An_import_one_topic_over_the_maximum_has_too_many()
    {
        _validator.TestValidate(Import(Limits.Topics.ImportMax + 1))
            .ShouldHaveValidationErrorFor("topics")
            .WithErrorMessage($"You can import at most {Limits.Topics.ImportMax} topics at once.")
            .Only();
    }

    [Fact]
    public void The_topics_of_an_import_that_is_too_big_are_not_checked()
    {
        ImportTopicsRequest import = Import(Limits.Topics.ImportMax + 1);
        import.Topics.First().Title = "";

        _validator.TestValidate(import)
            .ShouldHaveValidationErrorFor("topics")
            .WithErrorMessage($"You can import at most {Limits.Topics.ImportMax} topics at once.")
            .Only();
    }

    [Fact]
    public void Every_invalid_topic_is_reported_at_its_index()
    {
        ImportTopicsRequest import = Import(3);
        import.Topics.ElementAt(0).Title = "";
        import.Topics.ElementAt(2).Questions.First().Cost = 0;

        TestValidationResult<ImportTopicsRequest> result = _validator.TestValidate(import);

        result.ShouldHaveValidationErrorFor("topics[0].title").WithErrorMessage("The title is required.");
        result.ShouldHaveValidationErrorFor("topics[2].questions[0].cost")
            .WithErrorMessage($"Cost must be between {Limits.Topics.QuestionCostMin} and {Limits.Topics.QuestionCostMax}.");
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void A_missing_topic_is_an_error_and_the_others_are_still_checked()
    {
        ImportTopicsRequest import = Import(2);
        import.Topics.ElementAt(1).Title = "";
        import.Topics = [import.Topics.ElementAt(0), null!, import.Topics.ElementAt(1)];

        TestValidationResult<ImportTopicsRequest> result = _validator.TestValidate(import);

        result.ShouldHaveValidationErrorFor("topics[1]").WithErrorMessage("This topic is missing.");
        result.ShouldHaveValidationErrorFor("topics[2].title").WithErrorMessage("The title is required.");
        Assert.Equal(2, result.Errors.Count);
    }

    protected override TestValidationResult<ImportTopicsRequest> Validate(TopicData topic) =>
        _validator.TestValidate(new ImportTopicsRequest { IsAuthor = true, Topics = [ToRequest(topic)] });

    private static ImportTopicsRequest Import(int topicCount) =>
        new ImportTopicsRequest
        {
            IsAuthor = true,
            Topics = [.. Enumerable.Range(1, topicCount).Select(i => ToRequest(ValidTopic with { Title = $"Topic {i}" }))],
        };

    private static ImportTopicRequest ToRequest(TopicData topic) =>
        new ImportTopicRequest
        {
            Title = topic.Title,
            Description = topic.Description,
            Questions =
            [
                .. topic.Questions.Select(q => q is null ? null! : new CreateQuestionRequest
                {
                    QuestionNumber = q.QuestionNumber, Cost = q.Cost, Text = q.Text, Answer = q.Answer, Comment = q.Comment,
                }),
            ],
        };
}
