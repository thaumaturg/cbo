using System.Linq;
using Cbo.Core.Domain;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Tournaments;

/// <summary>
/// The rules that create and update tournament requests share. Each subclass builds its own request from a
/// <see cref="TournamentData"/>. A missing setting is checked with its default filled in.
/// </summary>
public abstract class TournamentRequestValidatorTests<TRequest>
{
    protected sealed record TournamentData(
        string Title,
        string? Description = null,
        int? TournamentSize = null,
        int? TopicsPerParticipantMax = null,
        int? TopicsPerParticipantMin = null);

    public static TheoryData<int> SupportedSizes => new(Limits.Tournaments.SupportedSizes);

    protected static TournamentData ValidTournament => new("Spring cup", "Played every April");

    protected abstract string InvalidMessage { get; }

    protected abstract TestValidationResult<TRequest> Validate(TournamentData tournament);

    [Fact]
    public void Invalid_message_is_about_the_tournament()
    {
        Assert.Equal("Some of the tournament data is invalid.", InvalidMessage);
    }

    [Fact]
    public void A_tournament_with_every_setting_left_out_is_valid()
    {
        Validate(ValidTournament).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_blank_title_is_required()
    {
        Validate(ValidTournament with { Title = "   " })
            .ShouldHaveValidationErrorFor("title").WithErrorMessage("The title is required.").Only();
    }

    // A JSON null for a required string reaches the validator as null
    [Fact]
    public void A_null_title_is_required()
    {
        Validate(ValidTournament with { Title = null! })
            .ShouldHaveValidationErrorFor("title").WithErrorMessage("The title is required.").Only();
    }

    [Fact]
    public void The_title_can_be_as_short_as_the_minimum()
    {
        Validate(ValidTournament with { Title = new string('t', Limits.Tournaments.TitleLengthMin) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_title_one_under_the_minimum_is_too_short()
    {
        Validate(ValidTournament with { Title = new string('t', Limits.Tournaments.TitleLengthMin - 1) })
            .ShouldHaveValidationErrorFor("title")
            .WithErrorMessage(TitleLengthMessage)
            .Only();
    }

    [Fact]
    public void The_title_can_be_as_long_as_the_maximum()
    {
        Validate(ValidTournament with { Title = new string('t', Limits.Tournaments.TitleLengthMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_title_one_over_the_maximum_is_too_long()
    {
        Validate(ValidTournament with { Title = new string('t', Limits.Tournaments.TitleLengthMax + 1) })
            .ShouldHaveValidationErrorFor("title")
            .WithErrorMessage(TitleLengthMessage)
            .Only();
    }

    [Fact]
    public void The_description_is_optional()
    {
        Validate(ValidTournament with { Description = null }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void The_description_can_be_as_long_as_the_maximum()
    {
        Validate(ValidTournament with { Description = new string('d', Limits.Tournaments.DescriptionLengthMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_description_one_over_the_maximum_is_too_long()
    {
        Validate(ValidTournament with { Description = new string('d', Limits.Tournaments.DescriptionLengthMax + 1) })
            .ShouldHaveValidationErrorFor("description")
            .WithErrorMessage($"The description can be at most {Limits.Tournaments.DescriptionLengthMax} characters.")
            .Only();
    }

    [Theory]
    [MemberData(nameof(SupportedSizes))]
    public void A_supported_tournament_size_is_valid(int size)
    {
        Validate(ValidTournament with { TournamentSize = size }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_unsupported_tournament_size_fails_on_tournament_size_and_lists_the_supported_sizes()
    {
        int unsupportedSize = Limits.Tournaments.SupportedSizes.Max() + 1;

        Validate(ValidTournament with { TournamentSize = unsupportedSize })
            .ShouldHaveValidationErrorFor("tournamentSize")
            .WithErrorMessage(
                $"Tournament size must be one of: {string.Join(", ", Limits.Tournaments.SupportedSizes)}.")
            .Only();
    }

    [Fact]
    public void The_minimum_can_be_as_low_as_the_floor()
    {
        Validate(ValidTournament with { TopicsPerParticipantMin = Limits.Tournaments.TopicsPerParticipantFloor })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_minimum_one_under_the_floor_is_negative()
    {
        Validate(ValidTournament with { TopicsPerParticipantMin = Limits.Tournaments.TopicsPerParticipantFloor - 1 })
            .ShouldHaveValidationErrorFor("topicsPerParticipantMin")
            .WithErrorMessage("The minimum number of topics can't be negative.")
            .Only();
    }

    // The minimum is only a recommendation (ADR 0007), so the maximum is its only bound from above
    [Fact]
    public void The_minimum_can_be_as_high_as_the_ceiling_when_the_maximum_is_too()
    {
        Validate(ValidTournament with
            {
                TopicsPerParticipantMin = Limits.Tournaments.TopicsPerParticipantCeiling,
                TopicsPerParticipantMax = Limits.Tournaments.TopicsPerParticipantCeiling,
            })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_minimum_one_over_the_ceiling_fails_only_on_the_maximum_and_names_the_minimum()
    {
        const int min = Limits.Tournaments.TopicsPerParticipantCeiling + 1;

        TestValidationResult<TRequest> result = Validate(ValidTournament with
        {
            TopicsPerParticipantMin = min,
            TopicsPerParticipantMax = Limits.Tournaments.TopicsPerParticipantCeiling,
        });

        result.ShouldNotHaveValidationErrorFor("topicsPerParticipantMin");
        result.ShouldHaveValidationErrorFor("topicsPerParticipantMax")
            .WithErrorMessage($"The maximum number of topics can't be less than the minimum ({min}).")
            .Only();
    }

    [Fact]
    public void The_maximum_can_be_as_low_as_the_floor_when_the_minimum_is_too()
    {
        Validate(ValidTournament with
            {
                TopicsPerParticipantMin = Limits.Tournaments.TopicsPerParticipantFloor,
                TopicsPerParticipantMax = Limits.Tournaments.TopicsPerParticipantFloor,
            })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_maximum_one_under_the_floor_is_negative()
    {
        Validate(ValidTournament with
            {
                TopicsPerParticipantMin = Limits.Tournaments.TopicsPerParticipantFloor,
                TopicsPerParticipantMax = Limits.Tournaments.TopicsPerParticipantFloor - 1,
            })
            .ShouldHaveValidationErrorFor("topicsPerParticipantMax")
            .WithErrorMessage("The maximum number of topics can't be negative.")
            .Only();
    }

    [Fact]
    public void The_maximum_can_be_as_high_as_the_ceiling()
    {
        Validate(ValidTournament with { TopicsPerParticipantMax = Limits.Tournaments.TopicsPerParticipantCeiling })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_maximum_one_over_the_ceiling_is_too_high()
    {
        Validate(ValidTournament with { TopicsPerParticipantMax = Limits.Tournaments.TopicsPerParticipantCeiling + 1 })
            .ShouldHaveValidationErrorFor("topicsPerParticipantMax")
            .WithErrorMessage(
                $"The maximum number of topics can be at most {Limits.Tournaments.TopicsPerParticipantCeiling}.")
            .Only();
    }

    [Fact]
    public void A_maximum_equal_to_the_minimum_is_valid()
    {
        Validate(ValidTournament with { TopicsPerParticipantMin = 7, TopicsPerParticipantMax = 7 })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_maximum_one_under_the_minimum_is_less_than_it_and_names_the_minimum()
    {
        Validate(ValidTournament with { TopicsPerParticipantMin = 7, TopicsPerParticipantMax = 6 })
            .ShouldHaveValidationErrorFor("topicsPerParticipantMax")
            .WithErrorMessage("The maximum number of topics can't be less than the minimum (7).")
            .Only();
    }

    [Fact]
    public void A_minimum_above_the_default_maximum_fails_on_the_maximum_when_the_maximum_is_left_out()
    {
        const int min = TournamentDefaults.TopicsPerParticipantMax + 1;

        Validate(ValidTournament with { TopicsPerParticipantMin = min })
            .ShouldHaveValidationErrorFor("topicsPerParticipantMax")
            .WithErrorMessage($"The maximum number of topics can't be less than the minimum ({min}).")
            .Only();
    }

    [Fact]
    public void A_maximum_below_the_default_minimum_names_the_default_when_the_minimum_is_left_out()
    {
        Validate(ValidTournament with { TopicsPerParticipantMax = TournamentDefaults.TopicsPerParticipantMin - 1 })
            .ShouldHaveValidationErrorFor("topicsPerParticipantMax")
            .WithErrorMessage(
                $"The maximum number of topics can't be less than the minimum ({TournamentDefaults.TopicsPerParticipantMin}).")
            .Only();
    }

    [Fact]
    public void Errors_on_different_fields_are_all_reported()
    {
        TestValidationResult<TRequest> result = Validate(new TournamentData(
            Title: "",
            Description: new string('d', Limits.Tournaments.DescriptionLengthMax + 1),
            TournamentSize: 0,
            TopicsPerParticipantMax: 3,
            TopicsPerParticipantMin: -1));

        result.ShouldHaveValidationErrorFor("title");
        result.ShouldHaveValidationErrorFor("description");
        result.ShouldHaveValidationErrorFor("tournamentSize");
        result.ShouldHaveValidationErrorFor("topicsPerParticipantMin");
        Assert.Equal(4, result.Errors.Count);
    }

    private static string TitleLengthMessage =>
        $"The title must be between {Limits.Tournaments.TitleLengthMin} and {Limits.Tournaments.TitleLengthMax} characters.";
}
