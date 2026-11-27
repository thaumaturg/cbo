using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Tests.Integration;

public class StartPlayoffTests(ApiFactory api)
{
    private static readonly string[] s_playersBySeed = ["S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12"];

    [Fact]
    public async Task Starting_the_playoff_creates_playoff_1_and_2_from_the_qualification_places()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartQualificationsAsync();
        await tournament.PlayQualificationsAsync(new Dictionary<string, int> { ["S12"] = 50 });

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Playoff);

        await TestTournament.Succeeded(response);
        Assert.Equal(nameof(TournamentStage.Playoff), (await tournament.GetAsync()).CurrentStage);

        List<MatchResponse> matches = await tournament.MatchesAsync();
        List<MatchResponse> playoff = [.. matches.Where(m => m.Type != MatchType.Qualification)];
        Assert.Equal([BracketPosition.Playoff1, BracketPosition.Playoff2], playoff.Select(m => m.BracketPosition));
        Assert.All(playoff, match => Assert.Equal(MatchType.Playoff, match.Type));

        // S12 scores in each of their matches and no one else does. S12 gets 3 points a match and those they beat 1;
        // the others share 1.5 each in matches without anyone scoring. So S12 is 1st on 9 points, S2 and S10, who
        // never met S12, follow on 4.5, and everyone else has 4 and stays in seed order.
        Assert.Equal(
            new Dictionary<string, string?> { ["S3"] = "Q5", ["S5"] = "Q7", ["S8"] = "Q10", ["S11"] = "Q12" },
            SourceLabels(playoff[0]));
        Assert.Equal(
            new Dictionary<string, string?> { ["S4"] = "Q6", ["S6"] = "Q8", ["S7"] = "Q9", ["S9"] = "Q11" },
            SourceLabels(playoff[1]));

        Assert.All(matches.Where(m => m.Type == MatchType.Qualification), match => Assert.True(match.IsLocked));
    }

    [Fact]
    public async Task The_playoff_matches_are_numbered_after_the_qualification_matches_by_bracket_position()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);

        await tournament.StartPlayoffWithPlacesBySeedAsync();

        List<MatchResponse> matches = await tournament.MatchesAsync();
        int qualificationMatches = matches.Count(m => m.Type == MatchType.Qualification);
        MatchResponse playoff2 = matches.Single(m => m.BracketPosition == BracketPosition.Playoff2);
        Assert.Equal(qualificationMatches + 2, playoff2.NumberInTournament);
        Assert.Equal(2, playoff2.NumberInStage);
        Assert.Equal(TournamentStage.Playoff, playoff2.CreatedOnStage);
    }

    [Fact]
    public async Task The_playoff_cant_start_until_every_qualification_match_is_finished()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartQualificationsAsync();
        MatchResponse first = (await tournament.MatchesAsync()).First();
        await tournament.PlayAsync(first, first.MatchParticipants.ToDictionary(TestTournament.NameOf, _ => 0));

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Playoff);

        string detail = await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Contains("8 qualification matches", detail);
    }

    [Fact]
    public async Task The_playoff_cant_start_from_preparations()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Playoff);

        await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
    }

    [Fact]
    public async Task The_playoff_cant_start_twice()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Playoff);

        await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
    }

    [Fact]
    public async Task Only_the_creator_can_start_the_playoff()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartQualificationsAsync();
        await tournament.PlayQualificationsAsync();

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Playoff, tournament.Player);

        await TestTournament.FailedWith(HttpStatusCode.Forbidden, response);
    }

    [Fact]
    public async Task A_format_without_a_playoff_bracket_cant_start_the_playoff()
    {
        string[] fourteen = [.. Enumerable.Range(1, 14).Select(seed => $"S{seed}")];
        TestTournament tournament = await TestTournament.CreateAsync(api, fourteen, TournamentFormat.Fourteen);
        await tournament.StartQualificationsAsync();
        await tournament.PlayQualificationsAsync();

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Playoff);

        string detail = await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Contains("bracket", detail);
    }

    private static Dictionary<string, string?> SourceLabels(MatchResponse match) =>
        match.MatchParticipants.ToDictionary(TestTournament.NameOf, p => p.SourceLabel);
}
