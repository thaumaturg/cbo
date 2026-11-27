using Cbo.Core.Domain;
using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Tournaments;

public sealed class ChangeStageRequestValidator : RequestValidator<ChangeStageRequest>
{
    public ChangeStageRequestValidator()
    {
        RuleFor(r => r.Stage)
            .Must(stage => stage is TournamentStage.Qualifications or TournamentStage.Playoff or TournamentStage.Ended)
            .WithMessage("The stage can only be set to Qualifications, Playoff or Ended.");
    }

    public override string InvalidMessage => "Some of the stage data is invalid.";
}
