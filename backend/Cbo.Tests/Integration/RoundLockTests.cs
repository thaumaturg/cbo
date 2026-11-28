using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Participants;

namespace Cbo.Tests.Integration;

public class RoundLockTests(ApiFactory api)
{
    // With no qualification scores, qualification places follow the seeds: S5 is Q5
    private static readonly string[] s_playersBySeed = ["S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12"];

    private const string ConfirmedReason = "match is confirmed";
    private const string PlayoffStartedReason = "Playoff has started";

    [Fact]
    public async Task Rounds_of_a_confirmed_playoff_match_cant_be_added_changed_or_deleted()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchDetailResponse playoff1 = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 40), ("S7", 30), ("S10", 20), ("S12", 10)));

        await AssertRoundsLockedAsync(tournament, playoff1.Id, ConfirmedReason);
    }

    [Fact]
    public async Task Rounds_of_a_confirmed_shootout_cant_be_added_changed_or_deleted()
    {
        TestTournament tournament = await InPlayoffAsync();
        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 30), ("S10", 30), ("S12", 10)));
        MatchResponse shootout = (await tournament.MatchesAsync()).Single(m => m.Type == MatchType.Shootout);
        await tournament.PlayAndConfirmAsync(shootout, Scores(("S7", 0), ("S10", 10)));

        await AssertRoundsLockedAsync(tournament, shootout.Id, ConfirmedReason);
    }

    [Fact]
    public async Task Rounds_of_a_confirmed_final_cant_be_added_changed_or_deleted()
    {
        TestTournament tournament = await InPlayoffAsync();
        await tournament.PlayOutPlayoffAsync();
        MatchResponse final = await tournament.MatchAsync(BracketPosition.Final);

        await AssertRoundsLockedAsync(tournament, final.Id, ConfirmedReason);
    }

    [Theory]
    [InlineData(TournamentStage.Playoff)]
    [InlineData(TournamentStage.Ended)]
    public async Task Rounds_of_a_qualification_match_cant_be_added_changed_or_deleted_once_the_playoff_has_started(TournamentStage stage)
    {
        TestTournament tournament = await InPlayoffAsync();
        if (stage == TournamentStage.Ended)
        {
            await tournament.PlayOutPlayoffAsync();
            await TestTournament.Succeeded(await tournament.ChangeStageAsync(TournamentStage.Ended));
        }

        MatchResponse qualification = (await tournament.MatchesAsync()).First(m => m.Type == MatchType.Qualification);

        await AssertRoundsLockedAsync(tournament, qualification.Id, PlayoffStartedReason);
    }

    [Fact]
    public async Task Playoff_matches_shootouts_and_the_final_get_scores_but_no_points_and_leave_the_tournament_totals_unchanged()
    {
        TestTournament tournament = await InPlayoffAsync();
        List<ParticipantResponse> playersBefore = await tournament.PlayersAsync();

        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 30), ("S10", 30), ("S12", 10)));
        MatchResponse shootout = (await tournament.MatchesAsync()).Single(m => m.Type == MatchType.Shootout);
        await tournament.PlayAndConfirmAsync(shootout, Scores(("S7", 0), ("S10", 10)));

        // Rounds of an unconfirmed playoff match are added, changed and deleted
        MatchResponse playoff2 = await tournament.MatchAsync(BracketPosition.Playoff2);
        await tournament.PlayAsync(playoff2, Scores(("S6", 10), ("S8", 20), ("S9", 30), ("S11", 40)));
        await TestTournament.Succeeded(await tournament.RescoreAsync(playoff2.Id, Scores(("S6", 40), ("S8", 30), ("S9", 20), ("S11", 10))));
        await TestTournament.Succeeded(await tournament.DeleteRoundAsync(playoff2.Id, 4));
        await TestTournament.Succeeded(await tournament.AddRoundAsync(playoff2.Id, 4));
        await TestTournament.Succeeded(await tournament.ConfirmAsync(playoff2.Id));

        await tournament.PlayOutPlayoffAsync();

        Assert.Equal(Totals(playersBefore), Totals(await tournament.PlayersAsync()));
        List<MatchParticipantResponse> playoffPlayers =
            [.. (await tournament.MatchesAsync()).Where(m => m.Type != MatchType.Qualification).SelectMany(m => m.MatchParticipants)];
        Assert.All(playoffPlayers, p => Assert.Null(p.PointsSum));
        Assert.All(playoffPlayers, p => Assert.NotNull(p.ScoreSum));
        Assert.All(playoffPlayers, p => Assert.NotNull(p.PositiveScoreSum));
    }

    /// <summary>Adding, changing and deleting a round of the match each fail for this reason, and leave its rounds as they were.</summary>
    private static async Task AssertRoundsLockedAsync(TestTournament tournament, Guid matchId, string reason)
    {
        MatchDetailResponse before = await tournament.MatchAsync(matchId);

        HttpResponseMessage added = await tournament.AddRoundAsync(matchId, 1);
        HttpResponseMessage changed = await tournament.RescoreAsync(matchId, new Dictionary<string, int>());
        HttpResponseMessage deleted = await tournament.DeleteRoundAsync(matchId, 1);

        foreach (HttpResponseMessage response in new[] { added, changed, deleted })
            Assert.Contains(reason, await TestTournament.FailedWith(HttpStatusCode.Conflict, response));

        MatchDetailResponse after = await tournament.MatchAsync(matchId);
        Assert.Equal(before.Rounds.Select(r => (r.NumberInMatch, r.TopicId)), after.Rounds.Select(r => (r.NumberInMatch, r.TopicId)));
        Assert.Equal(
            before.MatchParticipants.Select(p => (p.Id, p.ScoreSum, p.PositiveScoreSum)),
            after.MatchParticipants.Select(p => (p.Id, p.ScoreSum, p.PositiveScoreSum)));
    }

    private async Task<TestTournament> InPlayoffAsync()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();
        return tournament;
    }

    private static Dictionary<string, int> Scores(params (string Player, int Score)[] scores) =>
        scores.ToDictionary(entry => entry.Player, entry => entry.Score);

    private static List<(Guid, decimal?, int?, int?)> Totals(IEnumerable<ParticipantResponse> players) =>
        [.. players.OrderBy(p => p.Id).Select(p => (p.Id, p.PointsSum, p.ScoreSum, p.PositiveScoreSum))];
}
