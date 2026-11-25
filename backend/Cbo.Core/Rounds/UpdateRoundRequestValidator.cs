using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Rounds;

/// <summary>
/// Whether the round number in the URL matches the body is checked by <see cref="UpdateRoundCommandHandler"/>.
/// </summary>
public sealed class UpdateRoundRequestValidator : RequestValidator<UpdateRoundRequest>
{
    public UpdateRoundRequestValidator()
    {
        RuleFor(r => r.NumberInMatch).RoundNumber();
        RuleForEach(r => r.Answers).RoundAnswer(r => r.IsOverrideMode);
        RuleFor(r => r.Answers).OneAnswerPerParticipantAndQuestion();
        RuleFor(r => r.Answers).OneCorrectAnswerPerQuestion(r => r.IsOverrideMode);
    }

    public override string InvalidMessage => RoundFailures.InvalidMessage;
}
