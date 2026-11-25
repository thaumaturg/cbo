using System.Text.Json;
using Cbo.Core.Domain;
using Cbo.Core.Participants;
using Cbo.Tests.Validation;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Participants;

public class CreateParticipantRequestValidatorTests
{
    private readonly CreateParticipantRequestValidator _validator = new();

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
        _validator.TestValidate(new CreateParticipantRequest { Username = "bob", Role = role })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData(null)]
    public void A_blank_username_is_required(string? username)
    {
        _validator.TestValidate(new CreateParticipantRequest { Username = username!, Role = TournamentParticipantRole.Player })
            .ShouldHaveValidationErrorFor("username")
            .WithErrorMessage("Username is required.")
            .Only();
    }

    [Fact]
    public void An_undefined_role_is_unknown()
    {
        _validator.TestValidate(new CreateParticipantRequest { Username = "bob", Role = (TournamentParticipantRole)99 })
            .ShouldHaveValidationErrorFor("role")
            .WithErrorMessage("Unknown role. Allowed: Player, Organizer, Creator.")
            .Only();
    }

    [Fact]
    public void Nobody_can_be_added_as_a_creator()
    {
        _validator.TestValidate(new CreateParticipantRequest { Username = "bob", Role = TournamentParticipantRole.Creator })
            .ShouldHaveValidationErrorFor("role")
            .WithErrorMessage("A tournament has only one creator, so nobody can be added as a creator.")
            .Only();
    }

    [Fact]
    public void A_blank_username_and_a_wrong_role_are_reported_together()
    {
        TestValidationResult<CreateParticipantRequest> result = _validator.TestValidate(
            new CreateParticipantRequest { Username = "", Role = TournamentParticipantRole.Creator });

        result.ShouldHaveValidationErrorFor("username");
        result.ShouldHaveValidationErrorFor("role");
    }

    [Fact]
    public void The_username_is_trimmed()
    {
        CreateParticipantRequest request = JsonSerializer.Deserialize<CreateParticipantRequest>(
            """{ "username": "  bob\t", "role": 0 }""", JsonSerializerOptions.Web)!;

        Assert.Equal("bob", request.Username);
    }

    // The API reads enums by name, so an unknown name must reach the validator instead of failing as the body is read
    [Fact]
    public void An_unknown_role_name_is_unknown()
    {
        CreateParticipantRequest request = ApiJson.Read<CreateParticipantRequest>("""{ "username": "bob", "role": "Admin" }""");

        _validator.TestValidate(request)
            .ShouldHaveValidationErrorFor("role")
            .WithErrorMessage("Unknown role. Allowed: Player, Organizer, Creator.")
            .Only();
    }

    [Fact]
    public void A_role_name_is_read_in_any_case()
    {
        CreateParticipantRequest request = ApiJson.Read<CreateParticipantRequest>("""{ "username": "bob", "role": "organizer" }""");

        Assert.Equal(TournamentParticipantRole.Organizer, request.Role);
    }
}
