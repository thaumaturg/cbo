using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Participants;

/// <summary>
/// Checks only that no player is listed twice. Whether the list holds exactly the tournament's players is checked by
/// <see cref="UpdateSeedingCommandHandler"/>, with the same message.
/// </summary>
public sealed class UpdateSeedingRequestValidator : RequestValidator<UpdateSeedingRequest>
{
    public UpdateSeedingRequestValidator()
    {
        RuleFor(r => r.OrderedParticipantIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage(ParticipantFailures.SeedingListsEachPlayerOnceMessage);
    }

    public override string InvalidMessage => ParticipantFailures.InvalidSeedingMessage;
}
