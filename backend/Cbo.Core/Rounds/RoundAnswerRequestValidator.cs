using Cbo.Core.Domain;
using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Rounds;

/// <summary>
/// Checks one answer of a create or update round request against the round's scoring mode. The rules about the whole
/// list are in <see cref="RoundRequestRules"/>. Internal, so the validator scan doesn't register it: it needs the
/// mode, which only the round request knows.
/// </summary>
internal sealed class RoundAnswerRequestValidator : ChildValidator<IRoundAnswerRequest>
{
    public RoundAnswerRequestValidator(bool isOverrideMode)
    {
        if (isOverrideMode)
        {
            RuleFor(a => a.IsAnswerAccepted)
                .Null().WithMessage("In override mode, IsAnswerAccepted must be null for all answers.");

            RuleFor(a => a.OverrideCost)
                .NotNull().WithMessage("In override mode, OverrideCost must be provided for all answers.")
                .InclusiveBetween(Limits.Rounds.OverrideCostMin, Limits.Rounds.OverrideCostMax)
                .WithMessage(
                    $"The override cost must be between {Limits.Rounds.OverrideCostMin} and {Limits.Rounds.OverrideCostMax}.");
        }
        else
        {
            RuleFor(a => a.IsAnswerAccepted)
                .NotNull().WithMessage("In standard mode, IsAnswerAccepted must be provided for all answers.");

            RuleFor(a => a.OverrideCost)
                .Null().WithMessage("In standard mode, OverrideCost must be null for all answers.");
        }
    }
}
