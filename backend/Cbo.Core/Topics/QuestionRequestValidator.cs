using Cbo.Core.Domain;
using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Topics;

/// <summary>
/// Checks one question of a create, update or import request. The rules about the whole list are in
/// <see cref="TopicRequestRules.QuestionList{T, TQuestion}"/>.
/// </summary>
public sealed class QuestionRequestValidator : ChildValidator<IQuestionRequest>
{
    public QuestionRequestValidator()
    {
        RuleFor(q => q.Text)
            .NotEmpty().WithMessage("Question text is required.")
            .MaximumLength(Limits.Topics.QuestionTextLengthMax)
            .WithMessage($"Question text can be at most {Limits.Topics.QuestionTextLengthMax} characters.");

        RuleFor(q => q.Answer)
            .NotEmpty().WithMessage("Answer is required.")
            .MaximumLength(Limits.Topics.AnswerLengthMax)
            .WithMessage($"The answer can be at most {Limits.Topics.AnswerLengthMax} characters.");

        RuleFor(q => q.Comment)
            .MaximumLength(Limits.Topics.CommentLengthMax)
            .WithMessage($"The comment can be at most {Limits.Topics.CommentLengthMax} characters.");

        RuleFor(q => q.Cost)
            .InclusiveBetween(Limits.Topics.QuestionCostMin, Limits.Topics.QuestionCostMax)
            .WithMessage($"Cost must be between {Limits.Topics.QuestionCostMin} and {Limits.Topics.QuestionCostMax}.");
    }
}
