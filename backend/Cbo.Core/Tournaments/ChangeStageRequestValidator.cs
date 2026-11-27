using Cbo.Core.Domain;
using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Tournaments;

public sealed class ChangeStageRequestValidator : RequestValidator<ChangeStageRequest>
{
    public ChangeStageRequestValidator()
    {
        RuleFor(r => r.Stage)
            .IsInEnum()
            .WithMessage($"Unknown stage. Allowed: {string.Join(", ", Enum.GetNames<TournamentStage>())}.");
    }

    public override string InvalidMessage => "Some of the stage data is invalid.";
}
