using System.Text.Json;
using Cbo.Core.Domain;
using Cbo.Core.Tournaments;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Tournaments;

/// <summary>
/// Tournament requests trim their text as the JSON body is read, so the validators see the value that gets saved.
/// </summary>
public class TournamentRequestTrimmingTests
{
    [Fact]
    public void A_create_request_trims_its_title_and_turns_a_blank_description_into_null()
    {
        CreateTournamentRequest request = Read<CreateTournamentRequest>(
            """{ "title": "  Spring  cup\t", "description": " \n " }""");

        Assert.Equal("Spring  cup", request.Title);
        Assert.Null(request.Description);
    }

    [Fact]
    public void An_update_request_trims_its_title_and_description()
    {
        UpdateTournamentRequest request = Read<UpdateTournamentRequest>(
            """{ "title": " Spring cup ", "description": "  Played  every April \t" }""");

        Assert.Equal("Spring cup", request.Title);
        Assert.Equal("Played  every April", request.Description);
    }

    [Fact]
    public void A_null_title_stays_null_for_the_validator_to_report()
    {
        CreateTournamentRequest request = Read<CreateTournamentRequest>("""{ "title": null }""");

        Assert.Null(request.Title);
    }

    [Fact]
    public void A_padded_title_is_checked_at_its_trimmed_length()
    {
        string shortTitle = new('t', Limits.Tournaments.TitleLengthMin - 1);
        CreateTournamentRequest request = Read<CreateTournamentRequest>($$"""{ "title": "  {{shortTitle}}   " }""");

        new CreateTournamentRequestValidator().TestValidate(request)
            .ShouldHaveValidationErrorFor("title")
            .WithErrorMessage(
                $"The title must be between {Limits.Tournaments.TitleLengthMin} and {Limits.Tournaments.TitleLengthMax} characters.")
            .Only();
    }

    private static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)!;
}
