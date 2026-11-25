using Cbo.Core.Domain;
using Cbo.Core.Topics;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Topics;

public class QuestionRequestValidatorTests
{
    private static readonly string CostOutOfRange =
        $"Cost must be between {Limits.Topics.QuestionCostMin} and {Limits.Topics.QuestionCostMax}.";

    private readonly QuestionRequestValidator _validator = new QuestionRequestValidator();

    [Fact]
    public void A_complete_question_is_valid()
    {
        _validator.TestValidate(Question()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_question_being_updated_is_checked_the_same_way()
    {
        var question = new UpdateQuestionRequest
        {
            QuestionNumber = 1, Cost = Limits.Topics.QuestionCostMax + 1, Text = "", Answer = "Paris",
        };

        TestValidationResult<IQuestionRequest> result = _validator.TestValidate(question);

        result.ShouldHaveValidationErrorFor("text").WithErrorMessage("Question text is required.");
        result.ShouldHaveValidationErrorFor("cost").WithErrorMessage(CostOutOfRange);
    }

    [Fact]
    public void Text_is_required()
    {
        _validator.TestValidate(Question(text: ""))
            .ShouldHaveValidationErrorFor("text").WithErrorMessage("Question text is required.").Only();
    }

    // A JSON null for a required string reaches the validator as null
    [Fact]
    public void Null_text_and_answer_are_required()
    {
        TestValidationResult<IQuestionRequest> result = _validator.TestValidate(Question(text: null!, answer: null!));

        result.ShouldHaveValidationErrorFor("text").WithErrorMessage("Question text is required.");
        result.ShouldHaveValidationErrorFor("answer").WithErrorMessage("Answer is required.");
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Text_can_be_as_long_as_the_maximum()
    {
        _validator.TestValidate(Question(text: new string('q', Limits.Topics.QuestionTextLengthMax)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Text_one_over_the_maximum_is_too_long()
    {
        _validator.TestValidate(Question(text: new string('q', Limits.Topics.QuestionTextLengthMax + 1)))
            .ShouldHaveValidationErrorFor("text")
            .WithErrorMessage($"Question text can be at most {Limits.Topics.QuestionTextLengthMax} characters.")
            .Only();
    }

    [Fact]
    public void Answer_is_required()
    {
        _validator.TestValidate(Question(answer: ""))
            .ShouldHaveValidationErrorFor("answer").WithErrorMessage("Answer is required.").Only();
    }

    [Fact]
    public void Answer_can_be_as_long_as_the_maximum()
    {
        _validator.TestValidate(Question(answer: new string('a', Limits.Topics.AnswerLengthMax)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Answer_one_over_the_maximum_is_too_long()
    {
        _validator.TestValidate(Question(answer: new string('a', Limits.Topics.AnswerLengthMax + 1)))
            .ShouldHaveValidationErrorFor("answer")
            .WithErrorMessage($"The answer can be at most {Limits.Topics.AnswerLengthMax} characters.")
            .Only();
    }

    [Fact]
    public void Comment_is_optional()
    {
        _validator.TestValidate(Question(comment: null)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Comment_can_be_as_long_as_the_maximum()
    {
        _validator.TestValidate(Question(comment: new string('c', Limits.Topics.CommentLengthMax)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Comment_one_over_the_maximum_is_too_long()
    {
        _validator.TestValidate(Question(comment: new string('c', Limits.Topics.CommentLengthMax + 1)))
            .ShouldHaveValidationErrorFor("comment")
            .WithErrorMessage($"The comment can be at most {Limits.Topics.CommentLengthMax} characters.")
            .Only();
    }

    [Theory]
    [InlineData(Limits.Topics.QuestionCostMin)]
    [InlineData(Limits.Topics.QuestionCostMax)]
    public void Cost_at_either_end_of_the_range_is_valid(int cost)
    {
        _validator.TestValidate(Question(cost: cost)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(Limits.Topics.QuestionCostMin - 1)]
    [InlineData(Limits.Topics.QuestionCostMax + 1)]
    public void Cost_one_outside_the_range_is_invalid(int cost)
    {
        _validator.TestValidate(Question(cost: cost))
            .ShouldHaveValidationErrorFor("cost").WithErrorMessage(CostOutOfRange).Only();
    }

    private static CreateQuestionRequest Question(
        int cost = 10, string text = "Capital of France?", string answer = "Paris", string? comment = "Geography") =>
        new CreateQuestionRequest { QuestionNumber = 1, Cost = cost, Text = text, Answer = answer, Comment = comment };
}
