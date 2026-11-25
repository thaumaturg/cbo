using Cbo.Core.Domain;
using Cbo.Core.Participants;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Participants;

public class UpdateParticipantRequestValidatorTests
{
    private readonly UpdateParticipantRequestValidator _validator = new();

    [Fact]
    public void Invalid_message_is_about_the_participant()
    {
        Assert.Equal("Some of the participant data is invalid.", _validator.InvalidMessage);
    }

    [Theory]
    [InlineData(TournamentParticipantRole.Player)]
    [InlineData(TournamentParticipantRole.Organizer)]
    public void A_player_or_organizer_is_valid(TournamentParticipantRole role)
    {
        _validator.TestValidate(new UpdateParticipantRequest { Role = role }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_undefined_role_is_unknown()
    {
        _validator.TestValidate(new UpdateParticipantRequest { Role = (TournamentParticipantRole)(-1) })
            .ShouldHaveValidationErrorFor("role")
            .WithErrorMessage("Unknown role. Allowed: Player, Organizer, Creator.")
            .Only();
    }

    [Fact]
    public void Nobody_can_be_promoted_to_creator()
    {
        _validator.TestValidate(new UpdateParticipantRequest { Role = TournamentParticipantRole.Creator })
            .ShouldHaveValidationErrorFor("role")
            .WithErrorMessage("A participant can't be promoted to creator.")
            .Only();
    }
}
