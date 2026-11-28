using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Cbo.Core.Domain;

namespace Cbo.Tests.Integration;

/// <summary>Ending a tournament whose Final is decided is played through in <see cref="PlayoffThroughTheApiTests"/>.</summary>
public class EndTournamentTests(ApiFactory api)
{
    private static readonly string[] s_playersBySeed = ["S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12"];

    [Fact]
    public async Task The_tournament_cant_end_before_its_final_is_decided()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Ended);

        string detail = await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Contains("Final", detail);
    }

    [Fact]
    public async Task The_tournament_cant_end_from_qualifications()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartQualificationsAsync();

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Ended);

        string detail = await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Contains("Playoff stage", detail);
    }

    [Fact]
    public async Task Only_the_creator_can_end_the_tournament()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Ended, tournament.Player);

        await TestTournament.FailedWith(HttpStatusCode.Forbidden, response);
    }
}
