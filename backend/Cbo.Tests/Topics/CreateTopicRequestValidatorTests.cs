using System.Linq;
using Cbo.Core.Topics;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Identity;

namespace Cbo.Tests.Topics;

public class CreateTopicRequestValidatorTests : TopicRequestValidatorTests<CreateTopicRequest>
{
    private readonly CreateTopicRequestValidator _validator =
        new CreateTopicRequestValidator(new UpperInvariantLookupNormalizer());

    [Fact]
    public void Invalid_message_is_about_the_topic()
    {
        Assert.Equal("Some of the topic data is invalid.", _validator.InvalidMessage);
    }

    protected override TestValidationResult<CreateTopicRequest> Validate(TopicData topic) =>
        _validator.TestValidate(new CreateTopicRequest
        {
            Title = topic.Title,
            Description = topic.Description,
            IsAuthor = true,
            Questions =
            [
                .. topic.Questions.Select(q => q is null ? null! : new CreateQuestionRequest
                {
                    QuestionNumber = q.QuestionNumber, Cost = q.Cost, Text = q.Text, Answer = q.Answer, Comment = q.Comment,
                }),
            ],
            Authors = [.. topic.Authors.Select(a => a is null ? null! : new TopicAuthorRequest { Username = a.Username! })],
        });
}
