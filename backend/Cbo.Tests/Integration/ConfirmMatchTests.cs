using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Tests.Integration;

public class ConfirmMatchTests(ApiFactory api)
{
    // With no qualification scores, qualification places follow the seeds: S5 is Q5
    private static readonly string[] s_playersBySeed = ["S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12"];

    [Fact]
    public async Task Confirming_a_tied_match_creates_a_shootout_for_the_tied_players_only()
    {
        TestTournament tournament = await InPlayoffAsync();

        MatchDetailResponse confirmed = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 30), ("S10", 30), ("S12", 10)));

        Assert.NotNull(confirmed.ConfirmedAt);
        Assert.True(confirmed.IsLocked);
        Assert.All(confirmed.MatchParticipants, p => Assert.Null(p.Outcome));

        List<MatchResponse> matches = await tournament.MatchesAsync();
        MatchResponse shootout = Assert.Single(matches, m => m.Type == MatchType.Shootout);
        Assert.Equal(confirmed.Id, shootout.SettledMatchId);
        Assert.Equal(1, shootout.ShootoutNumber);
        Assert.Null(shootout.BracketPosition);
        Assert.Equal(new Dictionary<string, string?> { ["S7"] = "Q7", ["S10"] = "Q10" }, SourceLabels(shootout));
        // Numbered after every playoff match and the Final
        Assert.Equal(matches.Count(m => m.Type == MatchType.Qualification) + 11, shootout.NumberInTournament);
        Assert.Equal(11, shootout.NumberInStage);
        Assert.Equal([BracketPosition.Playoff1, BracketPosition.Playoff2], BracketPositions(matches));
    }

    [Fact]
    public async Task Confirming_the_last_shootout_decides_the_match_it_settles()
    {
        TestTournament tournament = await InPlayoffAsync();
        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 30), ("S10", 30), ("S12", 10)));
        MatchResponse shootout = (await tournament.MatchesAsync()).Single(m => m.Type == MatchType.Shootout);

        MatchDetailResponse confirmedShootout = await tournament.PlayAndConfirmAsync(shootout, Scores(("S7", 0), ("S10", 10)));

        Assert.True(confirmedShootout.IsLocked);
        MatchResponse playoff1 = await tournament.MatchAsync(BracketPosition.Playoff1);
        Assert.Equal(
            new Dictionary<string, Outcome?>
            {
                ["S5"] = Outcome.FirstWinner,
                ["S10"] = Outcome.SecondWinner,
                ["S7"] = Outcome.Loser,
                ["S12"] = Outcome.Loser,
            },
            Outcomes(playoff1));
        // Losers of #1 drop to #5, so nobody is out yet
        Assert.All(playoff1.MatchParticipants, p => Assert.False(p.IsEliminated));
        // The shootout's players show what they got from #1
        Assert.Equal(
            new Dictionary<string, Outcome?> { ["S7"] = Outcome.Loser, ["S10"] = Outcome.SecondWinner },
            Outcomes((await tournament.MatchesAsync()).Single(m => m.Id == shootout.Id)));
    }

    [Fact]
    public async Task Next_matches_are_created_once_every_match_they_take_players_from_is_decided()
    {
        TestTournament tournament = await InPlayoffAsync();

        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 40), ("S10", 30), ("S12", 10)));

        Assert.Equal([BracketPosition.Playoff1, BracketPosition.Playoff2], BracketPositions(await tournament.MatchesAsync()));

        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff2, Scores(("S6", 10), ("S8", 20), ("S9", 30), ("S11", 40)));

        List<MatchResponse> matches = await tournament.MatchesAsync();
        Assert.Equal(
            [BracketPosition.Playoff1, BracketPosition.Playoff2, BracketPosition.Playoff3, BracketPosition.Playoff4, BracketPosition.Playoff5],
            BracketPositions(matches));
        Assert.Equal(
            new Dictionary<string, string?> { ["S1"] = "Q1", ["S4"] = "Q4", ["S5"] = "1st of #1", ["S11"] = "1st of #2" },
            SourceLabels(matches.Single(m => m.BracketPosition == BracketPosition.Playoff3)));
        Assert.Equal(
            new Dictionary<string, string?> { ["S2"] = "Q2", ["S3"] = "Q3", ["S7"] = "2nd of #1", ["S9"] = "2nd of #2" },
            SourceLabels(matches.Single(m => m.BracketPosition == BracketPosition.Playoff4)));
        Assert.Equal(
            new Dictionary<string, string?>
            {
                ["S10"] = "dropped from #1",
                ["S12"] = "dropped from #1",
                ["S6"] = "dropped from #2",
                ["S8"] = "dropped from #2",
            },
            SourceLabels(matches.Single(m => m.BracketPosition == BracketPosition.Playoff5)));
    }

    [Fact]
    public async Task Confirming_both_sources_of_the_next_matches_at_once_creates_them_exactly_once()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchResponse playoff1 = await tournament.MatchAsync(BracketPosition.Playoff1);
        MatchResponse playoff2 = await tournament.MatchAsync(BracketPosition.Playoff2);
        await tournament.PlayAsync(playoff1, Scores(("S5", 50), ("S7", 40), ("S10", 30), ("S12", 10)));
        await tournament.PlayAsync(playoff2, Scores(("S6", 10), ("S8", 20), ("S9", 30), ("S11", 40)));

        HttpResponseMessage[] responses = await Task.WhenAll(tournament.ConfirmAsync(playoff1.Id), tournament.ConfirmAsync(playoff2.Id));

        foreach (HttpResponseMessage response in responses)
            await TestTournament.Succeeded(response);

        Assert.Equal(
            [BracketPosition.Playoff1, BracketPosition.Playoff2, BracketPosition.Playoff3, BracketPosition.Playoff4, BracketPosition.Playoff5],
            BracketPositions(await tournament.MatchesAsync()));
    }

    [Fact]
    public async Task An_unfinished_match_cant_be_confirmed()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchResponse playoff1 = await tournament.MatchAsync(BracketPosition.Playoff1);

        HttpResponseMessage response = await tournament.ConfirmAsync(playoff1.Id);

        string detail = await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Contains("finished", detail);
    }

    [Fact]
    public async Task A_match_cant_be_confirmed_twice()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchDetailResponse playoff1 = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 40), ("S10", 30), ("S12", 10)));

        HttpResponseMessage response = await tournament.ConfirmAsync(playoff1.Id);

        string detail = await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Contains("already confirmed", detail);
    }

    [Fact]
    public async Task A_qualification_match_isnt_confirmed()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchResponse qualification = (await tournament.MatchesAsync()).First(m => m.Type == MatchType.Qualification);

        HttpResponseMessage response = await tournament.ConfirmAsync(qualification.Id);

        await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
    }

    [Fact]
    public async Task Matches_are_confirmed_only_in_the_playoff_stage()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartQualificationsAsync();
        await tournament.PlayQualificationsAsync();
        MatchResponse qualification = (await tournament.MatchesAsync()).First();

        HttpResponseMessage response = await tournament.ConfirmAsync(qualification.Id);

        string detail = await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Contains("Playoff stage", detail);
    }

    [Fact]
    public async Task Only_the_creator_can_confirm_a_match()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchResponse playoff1 = await tournament.MatchAsync(BracketPosition.Playoff1);
        await tournament.PlayAsync(playoff1, Scores(("S5", 50), ("S7", 40), ("S10", 30), ("S12", 10)));

        HttpResponseMessage response = await tournament.ConfirmAsync(playoff1.Id, tournament.Player);

        await TestTournament.FailedWith(HttpStatusCode.Forbidden, response);
    }

    [Fact]
    public async Task Confirming_a_match_of_another_tournament_is_not_found()
    {
        TestTournament tournament = await InPlayoffAsync();

        HttpResponseMessage response = await tournament.ConfirmAsync(System.Guid.NewGuid());

        await TestTournament.FailedWith(HttpStatusCode.NotFound, response);
    }

    private async Task<TestTournament> InPlayoffAsync()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();
        return tournament;
    }

    private static Dictionary<string, int> Scores(params (string Player, int Score)[] scores) =>
        scores.ToDictionary(entry => entry.Player, entry => entry.Score);

    private static List<BracketPosition?> BracketPositions(IEnumerable<MatchResponse> matches) =>
        [.. matches.Where(m => m.BracketPosition is not null).Select(m => m.BracketPosition).Order()];

    private static Dictionary<string, string?> SourceLabels(MatchResponse match) =>
        match.MatchParticipants.ToDictionary(TestTournament.NameOf, p => p.SourceLabel);

    private static Dictionary<string, Outcome?> Outcomes(MatchResponse match) =>
        match.MatchParticipants.ToDictionary(TestTournament.NameOf, p => p.Outcome);
}
