using Cbo.Core.Validation;

namespace Cbo.Core.Participants;

public sealed class UpdateParticipantRequestValidator : RequestValidator<UpdateParticipantRequest>
{
    public UpdateParticipantRequestValidator()
    {
        RuleFor(r => r.Role).AssignableRole("A participant can't be promoted to creator.");
    }

    public override string InvalidMessage => ParticipantFailures.InvalidMessage;
}
