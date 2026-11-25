using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Tournaments;

public sealed class CreateTournamentRequestValidator : RequestValidator<CreateTournamentRequest>
{
    public CreateTournamentRequestValidator()
    {
        RuleFor(t => t.Title).TournamentTitle();
        RuleFor(t => t.Description).TournamentDescription();
        RuleFor(t => t.TournamentSize).TournamentSize();
        RuleFor(t => t.TopicsPerParticipantMin).TopicsPerParticipantMin();
        RuleFor(t => t.TopicsPerParticipantMax).TopicsPerParticipantMax(t => t.TopicsPerParticipantMin);
    }

    public override string InvalidMessage => TournamentFailures.InvalidMessage;
}
