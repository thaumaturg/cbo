using System;
using Cbo.Core.Participants;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Participants;

public class UpdateSeedingRequestValidatorTests
{
    private static readonly Guid AliceId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BobId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private readonly UpdateSeedingRequestValidator _validator = new();

    [Fact]
    public void Invalid_message_is_about_the_seeding()
    {
        Assert.Equal("Some of the seeding data is invalid.", _validator.InvalidMessage);
    }

    [Fact]
    public void Each_player_listed_once_is_valid()
    {
        _validator.TestValidate(new UpdateSeedingRequest { OrderedParticipantIds = [BobId, AliceId] })
            .ShouldNotHaveAnyValidationErrors();
    }

    // Whether the list matches the tournament's players is the handler's check
    [Fact]
    public void An_empty_seeding_is_left_to_the_handler()
    {
        _validator.TestValidate(new UpdateSeedingRequest { OrderedParticipantIds = [] })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_player_listed_twice_is_an_error()
    {
        _validator.TestValidate(new UpdateSeedingRequest { OrderedParticipantIds = [AliceId, BobId, AliceId] })
            .ShouldHaveValidationErrorFor("orderedParticipantIds")
            .WithErrorMessage("The seeding must list each player of the tournament exactly once.")
            .Only();
    }
}
