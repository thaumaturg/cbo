using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Tournaments;

public sealed class UpdateTournamentRequestValidator : RequestValidator<UpdateTournamentRequest>
{
    public UpdateTournamentRequestValidator()
    {
        RuleFor(t => t.Title).TournamentTitle();
        RuleFor(t => t.Description).TournamentDescription();
        RuleFor(t => t.Format).SelectableFormat();
        RuleFor(t => t.TopicsPerParticipantMin).TopicsPerParticipantMin();
        RuleFor(t => t.TopicsPerParticipantMax).TopicsPerParticipantMax(t => t.TopicsPerParticipantMin);
    }

    public override string InvalidMessage => TournamentFailures.InvalidMessage;
}
