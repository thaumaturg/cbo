using Cbo.Core.Domain;
using Cbo.Core.Tournaments;

namespace Cbo.Tests.Tournaments;

public class TournamentSettingsTests
{
    [Fact]
    public void Settings_left_out_get_their_defaults()
    {
        TournamentSettings settings = TournamentSettings.Resolve(null, null, null);

        Assert.Equal(
            new TournamentSettings(
                TournamentDefaults.PlayersPerTournament,
                TournamentDefaults.TopicsPerParticipantMax,
                TournamentDefaults.TopicsPerParticipantMin),
            settings);
    }

    [Fact]
    public void Settings_given_are_kept()
    {
        TournamentSettings settings = TournamentSettings.Resolve(14, 30, 0);

        Assert.Equal(new TournamentSettings(14, 30, 0), settings);
    }
}
