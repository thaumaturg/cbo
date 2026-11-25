using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Participants;

public sealed class CreateParticipantRequestValidator : RequestValidator<CreateParticipantRequest>
{
    public CreateParticipantRequestValidator()
    {
        RuleFor(r => r.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(r => r.Role).AssignableRole("A tournament has only one creator, so nobody can be added as a creator.");
    }

    public override string InvalidMessage => ParticipantFailures.InvalidMessage;
}
