using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Rounds;

public sealed class CreateRoundRequestValidator : RequestValidator<CreateRoundRequest>
{
    public CreateRoundRequestValidator()
    {
        RuleFor(r => r.NumberInMatch).RoundNumber();
        RuleForEach(r => r.Answers).RoundAnswer(r => r.IsOverrideMode);
        RuleFor(r => r.Answers).OneAnswerPerParticipantAndQuestion();
        RuleFor(r => r.Answers).OneCorrectAnswerPerQuestion(r => r.IsOverrideMode);
    }

    public override string InvalidMessage => RoundFailures.InvalidMessage;
}
