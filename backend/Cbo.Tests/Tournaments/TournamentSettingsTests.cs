using System.Collections.Generic;
using System.Linq;
using Cbo.Core.Domain;
using Cbo.Core.Tournaments;
using Cbo.Results;

namespace Cbo.Tests.Tournaments;

public class TournamentSettingsTests
{
    public static TheoryData<int> SupportedSizes => new(Limits.Tournaments.SupportedSizes);

    [Fact]
    public void The_default_settings_are_valid()
    {
        List<FieldError> errors = TournamentSettings.Resolve(null, null, null).Validate();

        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(SupportedSizes))]
    public void A_supported_tournament_size_is_valid(int size)
    {
        List<FieldError> errors = TournamentSettings.Resolve(size, null, null).Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void An_unsupported_tournament_size_fails_on_players_per_tournament_and_lists_the_supported_sizes()
    {
        int unsupportedSize = Limits.Tournaments.SupportedSizes.Max() + 1;

        List<FieldError> errors = TournamentSettings.Resolve(unsupportedSize, null, null).Validate();

        FieldError error = Assert.Single(errors);
        Assert.Equal("playersPerTournament", error.Field);
        Assert.Equal(
            $"Tournament size must be one of: {string.Join(", ", Limits.Tournaments.SupportedSizes)}.",
            error.Message);
    }
}
