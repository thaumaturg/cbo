using System;
using System.Linq;
using Cbo.Core.Topics;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Identity;

namespace Cbo.Tests.Topics;

public class UpdateTopicRequestValidatorTests : TopicRequestValidatorTests<UpdateTopicRequest>
{
    private readonly UpdateTopicRequestValidator _validator =
        new UpdateTopicRequestValidator(new UpperInvariantLookupNormalizer());

    [Fact]
    public void Invalid_message_is_about_the_topic()
    {
        Assert.Equal("Some of the topic data is invalid.", _validator.InvalidMessage);
    }

    // Whether an id belongs to the topic needs the database, so the handler checks it
    [Fact]
    public void Question_ids_are_left_to_the_handler()
    {
        Guid id = Guid.NewGuid();

        TestValidationResult<UpdateTopicRequest> result = _validator.TestValidate(new UpdateTopicRequest
        {
            Title = "Geography",
            IsAuthor = true,
            Questions =
            [
                new UpdateQuestionRequest { Id = id, QuestionNumber = 1, Cost = 10, Text = "Capital of France?", Answer = "Paris" },
                new UpdateQuestionRequest { Id = id, QuestionNumber = 2, Cost = 10, Text = "Capital of Spain?", Answer = "Madrid" },
            ],
            Authors = [],
        });

        result.ShouldNotHaveAnyValidationErrors();
    }

    protected override TestValidationResult<UpdateTopicRequest> Validate(TopicData topic) =>
        _validator.TestValidate(new UpdateTopicRequest
        {
            Title = topic.Title,
            Description = topic.Description,
            IsAuthor = true,
            Questions =
            [
                .. topic.Questions.Select(q => new UpdateQuestionRequest
                {
                    QuestionNumber = q.QuestionNumber, Cost = q.Cost, Text = q.Text, Answer = q.Answer, Comment = q.Comment,
                }),
            ],
            Authors = [.. topic.Authors.Select(username => new TopicAuthorRequest { Username = username })],
        });
}
