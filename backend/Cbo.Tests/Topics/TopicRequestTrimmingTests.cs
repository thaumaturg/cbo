using System.Linq;
using System.Text.Json;
using Cbo.Core.Topics;

namespace Cbo.Tests.Topics;

/// <summary>
/// Topic requests trim their text as the JSON body is read, so the validators see the value that gets saved.
/// </summary>
public class TopicRequestTrimmingTests
{
    private const string PaddedQuestion =
        """{ "questionNumber": 1, "cost": 10, "text": "  Capital  of France? ", "answer": "\tParis\n", "comment": "  " }""";

    private const string PaddedUpdatedQuestion =
        """{ "questionNumber": 1, "cost": 10, "text": "  Capital  of France? ", "answer": "\tParis\n", "comment": " Europe " }""";

    [Fact]
    public void A_create_request_trims_its_text_and_turns_blank_optional_text_into_null()
    {
        CreateTopicRequest request = Read<CreateTopicRequest>($$"""
            {
              "title": "  World  geography ",
              "description": " \t ",
              "isAuthor": true,
              "questions": [{{PaddedQuestion}}],
              "authors": [{ "username": " anna " }]
            }
            """);

        Assert.Equal("World  geography", request.Title);
        Assert.Null(request.Description);
        CreateQuestionRequest question = Assert.Single(request.Questions);
        Assert.Equal("Capital  of France?", question.Text);
        Assert.Equal("Paris", question.Answer);
        Assert.Null(question.Comment);
        Assert.Equal("anna", Assert.Single(request.Authors).Username);
    }

    [Fact]
    public void An_update_request_trims_its_text()
    {
        UpdateTopicRequest request = Read<UpdateTopicRequest>($$"""
            {
              "title": " Geography ",
              "description": "  Capitals of Europe  ",
              "isAuthor": true,
              "questions": [{{PaddedUpdatedQuestion}}],
              "authors": [{ "username": "\tbob " }]
            }
            """);

        Assert.Equal("Geography", request.Title);
        Assert.Equal("Capitals of Europe", request.Description);
        UpdateQuestionRequest question = Assert.Single(request.Questions);
        Assert.Equal("Capital  of France?", question.Text);
        Assert.Equal("Paris", question.Answer);
        Assert.Equal("Europe", question.Comment);
        Assert.Equal("bob", Assert.Single(request.Authors).Username);
    }

    [Fact]
    public void An_import_request_trims_the_text_of_each_topic()
    {
        ImportTopicsRequest request = Read<ImportTopicsRequest>($$"""
            {
              "isAuthor": false,
              "topics": [{ "title": " Geography ", "description": "", "questions": [{{PaddedQuestion}}] }]
            }
            """);

        ImportTopicRequest topic = Assert.Single(request.Topics);
        Assert.Equal("Geography", topic.Title);
        Assert.Null(topic.Description);
        Assert.Equal("Capital  of France?", topic.Questions.Single().Text);
    }

    [Fact]
    public void A_null_title_stays_null_for_the_validator_to_report()
    {
        CreateTopicRequest request = Read<CreateTopicRequest>(
            """{ "title": null, "isAuthor": true, "questions": [], "authors": [] }""");

        Assert.Null(request.Title);
    }

    private static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)!;
}
