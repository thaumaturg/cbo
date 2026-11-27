using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Playoffs;

namespace Cbo.Tests.Integration;

public class ReopenMatchTests(ApiFactory api)
{
    // With no qualification scores, qualification places follow the seeds: S5 is Q5
    private static readonly string[] s_playersBySeed = ["S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12"];

    [Fact]
    public async Task Reopening_a_match_deletes_every_match_after_it_and_confirming_it_again_recreates_the_next_ones()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchDetailResponse playoff1 = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 40), ("S10", 30), ("S12", 10)));
        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff2, Scores(("S6", 10), ("S8", 20), ("S9", 30), ("S11", 40)));
        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff3, Scores(("S1", 40), ("S4", 30), ("S5", 20), ("S11", 10)));
        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff4, Scores(("S2", 40), ("S3", 30), ("S7", 20), ("S9", 10)));
        Assert.Equal(
            [BracketPosition.Playoff1, BracketPosition.Playoff2, BracketPosition.Playoff3, BracketPosition.Playoff4,
                BracketPosition.Playoff5, BracketPosition.Playoff6, BracketPosition.Playoff8],
            BracketPositions(await tournament.MatchesAsync()));

        MatchDetailResponse reopened = await tournament.ReopenedAsync(playoff1.Id);

        Assert.Null(reopened.ConfirmedAt);
        Assert.False(reopened.IsLocked);
        Assert.Equal(4, reopened.Rounds.Count);
        Assert.All(reopened.MatchParticipants, p => Assert.Null(p.Outcome));
        List<MatchResponse> matches = await tournament.MatchesAsync();
        Assert.Equal([BracketPosition.Playoff1, BracketPosition.Playoff2], BracketPositions(matches));
        MatchResponse playoff2 = matches.Single(m => m.BracketPosition == BracketPosition.Playoff2);
        Assert.NotNull(playoff2.ConfirmedAt);
        Assert.All(playoff2.MatchParticipants, p => Assert.NotNull(p.Outcome));

        await TestTournament.Succeeded(await tournament.ConfirmAsync(playoff1.Id));

        matches = await tournament.MatchesAsync();
        Assert.Equal(
            [BracketPosition.Playoff1, BracketPosition.Playoff2, BracketPosition.Playoff3, BracketPosition.Playoff4, BracketPosition.Playoff5],
            BracketPositions(matches));
        Assert.Equal(["S1", "S11", "S4", "S5"], Players(matches.Single(m => m.BracketPosition == BracketPosition.Playoff3)));
        Assert.All(matches.Where(m => m.BracketPosition > BracketPosition.Playoff2), m => Assert.Equal(0, m.RoundsCount));
    }

    [Fact]
    public async Task Reopening_a_shootout_deletes_the_shootouts_after_it_and_what_its_match_created()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchDetailResponse playoff1 = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 30), ("S7", 30), ("S10", 30), ("S12", 10)));
        MatchDetailResponse shootout1 = await tournament.PlayAndConfirmAsync(
            await ShootoutAsync(tournament, playoff1.Id, 1), Scores(("S5", 10), ("S7", 0), ("S10", 0)));
        await tournament.PlayAndConfirmAsync(await ShootoutAsync(tournament, playoff1.Id, 2), Scores(("S7", 10), ("S10", 0)));
        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff2, Scores(("S6", 10), ("S8", 20), ("S9", 30), ("S11", 40)));
        Assert.Equal(
            [BracketPosition.Playoff1, BracketPosition.Playoff2, BracketPosition.Playoff3, BracketPosition.Playoff4, BracketPosition.Playoff5],
            BracketPositions(await tournament.MatchesAsync()));

        MatchDetailResponse reopened = await tournament.ReopenedAsync(shootout1.Id);

        Assert.Null(reopened.ConfirmedAt);
        Assert.Single(reopened.Rounds);
        List<MatchResponse> matches = await tournament.MatchesAsync();
        Assert.Equal([BracketPosition.Playoff1, BracketPosition.Playoff2], BracketPositions(matches));
        Assert.Equal([1], matches.Where(m => m.SettledMatchId == playoff1.Id).Select(m => m.ShootoutNumber));
        MatchResponse playoff1After = matches.Single(m => m.Id == playoff1.Id);
        Assert.NotNull(playoff1After.ConfirmedAt);
        Assert.All(playoff1After.MatchParticipants, p => Assert.Null(p.Outcome));
        Assert.NotNull(matches.Single(m => m.BracketPosition == BracketPosition.Playoff2).ConfirmedAt);

        await TestTournament.Succeeded(await tournament.ConfirmAsync(shootout1.Id));

        MatchResponse shootout2 = await ShootoutAsync(tournament, playoff1.Id, 2);
        Assert.Equal(["S10", "S7"], Players(shootout2));
        Assert.Equal(0, shootout2.RoundsCount);
    }

    [Fact]
    public async Task Reopening_a_match_deletes_its_shootouts_even_unconfirmed_ones()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchDetailResponse playoff1 = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 30), ("S10", 30), ("S12", 10)));
        await tournament.PlayAsync(await ShootoutAsync(tournament, playoff1.Id, 1), Scores(("S7", 10), ("S10", 0)));

        await tournament.ReopenedAsync(playoff1.Id);

        Assert.DoesNotContain(await tournament.MatchesAsync(), m => m.Type == MatchType.Shootout);
    }

    [Fact]
    public async Task Reopening_the_final_clears_its_places_and_unlocks_it()
    {
        TestTournament tournament = await InPlayoffAsync();
        await tournament.PlayOutPlayoffAsync();
        MatchResponse final = await tournament.MatchAsync(BracketPosition.Final);
        Assert.All(final.MatchParticipants, p => Assert.NotNull(p.Outcome));
        List<Guid> existing = [.. (await tournament.MatchesAsync()).Select(m => m.Id).Order()];

        MatchDetailResponse reopened = await tournament.ReopenedAsync(final.Id);

        Assert.False(reopened.IsLocked);
        Assert.All(reopened.MatchParticipants, p => Assert.Null(p.Outcome));
        Assert.Equal(existing, (await tournament.MatchesAsync()).Select(m => m.Id).Order());
    }

    [Fact]
    public async Task An_unconfirmed_match_cant_be_reopened()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchResponse playoff1 = await tournament.MatchAsync(BracketPosition.Playoff1);
        await tournament.PlayAsync(playoff1, Scores(("S5", 50), ("S7", 40), ("S10", 30), ("S12", 10)));

        HttpResponseMessage response = await tournament.ReopenAsync(playoff1.Id);

        string detail = await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Contains("confirmed", detail);
    }

    [Fact]
    public async Task A_qualification_match_isnt_reopened()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchResponse qualification = (await tournament.MatchesAsync()).First(m => m.Type == MatchType.Qualification);

        HttpResponseMessage response = await tournament.ReopenAsync(qualification.Id);

        await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
    }

    [Fact]
    public async Task Matches_of_an_ended_tournament_cant_be_reopened()
    {
        TestTournament tournament = await InPlayoffAsync();
        await tournament.PlayOutPlayoffAsync();
        await TestTournament.Succeeded(await tournament.ChangeStageAsync(TournamentStage.Ended));
        MatchResponse final = await tournament.MatchAsync(BracketPosition.Final);

        HttpResponseMessage response = await tournament.ReopenAsync(final.Id);

        string detail = await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Contains("Playoff stage", detail);
        Assert.NotNull((await tournament.MatchAsync(BracketPosition.Final)).ConfirmedAt);
    }

    [Fact]
    public async Task Only_the_creator_can_reopen_a_match()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchDetailResponse playoff1 = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 40), ("S10", 30), ("S12", 10)));

        HttpResponseMessage response = await tournament.ReopenAsync(playoff1.Id, tournament.Player);

        await TestTournament.FailedWith(HttpStatusCode.Forbidden, response);
    }

    [Fact]
    public async Task Reopening_a_match_of_another_tournament_is_not_found()
    {
        TestTournament tournament = await InPlayoffAsync();

        HttpResponseMessage response = await tournament.ReopenAsync(Guid.NewGuid());

        await TestTournament.FailedWith(HttpStatusCode.NotFound, response);
    }

    [Fact]
    public async Task Topics_played_in_deleted_rounds_are_offered_again()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchDetailResponse playoff1 = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 40), ("S10", 30), ("S12", 10)));
        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff2, Scores(("S6", 10), ("S8", 20), ("S9", 30), ("S11", 40)));
        MatchResponse playoff3 = await tournament.MatchAsync(BracketPosition.Playoff3);
        await tournament.PlayAsync(playoff3, Scores(("S1", 40), ("S4", 30), ("S5", 20), ("S11", 10)));
        List<Guid> playedInPlayoff3 = [.. (await tournament.MatchAsync(playoff3.Id)).Rounds.Select(r => r.TopicId)];
        Assert.DoesNotContain(await AvailableTopicsAsync(tournament, playoff1.Id), playedInPlayoff3.Contains);

        await tournament.ReopenedAsync(playoff1.Id);

        Assert.Subset((await AvailableTopicsAsync(tournament, playoff1.Id)).ToHashSet(), playedInPlayoff3.ToHashSet());
    }

    [Fact]
    public async Task What_reopening_deletes_is_listed_with_round_counts_before_reopening()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchDetailResponse playoff1 = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 50), ("S7", 40), ("S10", 30), ("S12", 10)));
        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff2, Scores(("S6", 10), ("S8", 20), ("S9", 30), ("S11", 40)));
        await tournament.PlayAndConfirmAsync(BracketPosition.Playoff3, Scores(("S1", 40), ("S4", 30), ("S5", 30), ("S11", 10)));

        List<ReopenDeletionResponse> deletions = await ReopenDeletionsAsync(tournament, playoff1.Id);

        (MatchType, BracketPosition?, BracketPosition?, int?, int)[] expected =
        [
            (MatchType.Playoff, BracketPosition.Playoff3, null, null, 4),
            (MatchType.Shootout, null, BracketPosition.Playoff3, 1, 0),
            (MatchType.Playoff, BracketPosition.Playoff4, null, null, 0),
            (MatchType.Playoff, BracketPosition.Playoff5, null, null, 0),
        ];
        Assert.Equal(
            expected,
            deletions.Select(d => (d.Type, d.BracketPosition, d.SettledBracketPosition, d.ShootoutNumber, d.RoundsCount)));
        List<Guid> existing = [.. (await tournament.MatchesAsync()).Select(m => m.Id)];

        await tournament.ReopenedAsync(playoff1.Id);

        Assert.Equal(
            existing.Except(deletions.Select(d => d.MatchId)).Order(),
            (await tournament.MatchesAsync()).Select(m => m.Id).Order());
    }

    [Fact]
    public async Task What_reopening_a_shootout_deletes_starts_with_the_shootouts_after_it()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchDetailResponse playoff1 = await tournament.PlayAndConfirmAsync(
            BracketPosition.Playoff1, Scores(("S5", 30), ("S7", 30), ("S10", 30), ("S12", 10)));
        MatchDetailResponse shootout1 = await tournament.PlayAndConfirmAsync(
            await ShootoutAsync(tournament, playoff1.Id, 1), Scores(("S5", 10), ("S7", 0), ("S10", 0)));

        List<ReopenDeletionResponse> deletions = await ReopenDeletionsAsync(tournament, shootout1.Id);

        ReopenDeletionResponse shootout2 = Assert.Single(deletions);
        Assert.Equal<(MatchType, BracketPosition?, int?, int)>(
            (MatchType.Shootout, BracketPosition.Playoff1, 2, 0),
            (shootout2.Type, shootout2.SettledBracketPosition, shootout2.ShootoutNumber, shootout2.RoundsCount));
    }

    [Fact]
    public async Task What_reopening_deletes_is_listed_only_for_matches_that_can_be_reopened()
    {
        TestTournament tournament = await InPlayoffAsync();
        MatchResponse playoff1 = await tournament.MatchAsync(BracketPosition.Playoff1);

        HttpResponseMessage unconfirmed = await GetReopenDeletionsAsync(tournament, playoff1.Id);
        HttpResponseMessage byPlayer = await GetReopenDeletionsAsync(tournament, playoff1.Id, tournament.Player);

        await TestTournament.FailedWith(HttpStatusCode.Conflict, unconfirmed);
        await TestTournament.FailedWith(HttpStatusCode.Forbidden, byPlayer);
    }

    private async Task<TestTournament> InPlayoffAsync()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();
        return tournament;
    }

    private static async Task<MatchResponse> ShootoutAsync(TestTournament tournament, Guid settledMatchId, int number) =>
        (await tournament.MatchesAsync()).Single(m => m.SettledMatchId == settledMatchId && m.ShootoutNumber == number);

    private static async Task<List<Guid>> AvailableTopicsAsync(TestTournament tournament, Guid matchId) =>
        [.. (await tournament.Creator.GetFromJsonAsync<List<AvailableTopicResponse>>(
            $"/api/Tournaments/{tournament.Id}/matches/{matchId}/available-topics", TestTournament.JsonOptions))!.Select(t => t.TopicId)];

    private static Task<HttpResponseMessage> GetReopenDeletionsAsync(TestTournament tournament, Guid matchId, HttpClient? client = null) =>
        (client ?? tournament.Creator).GetAsync($"/api/Tournaments/{tournament.Id}/matches/{matchId}/reopen-deletions");

    private static async Task<List<ReopenDeletionResponse>> ReopenDeletionsAsync(TestTournament tournament, Guid matchId)
    {
        HttpResponseMessage response = await GetReopenDeletionsAsync(tournament, matchId);
        await TestTournament.Succeeded(response);
        return (await response.Content.ReadFromJsonAsync<List<ReopenDeletionResponse>>(TestTournament.JsonOptions))!;
    }

    private static Dictionary<string, int> Scores(params (string Player, int Score)[] scores) =>
        scores.ToDictionary(entry => entry.Player, entry => entry.Score);

    private static List<BracketPosition?> BracketPositions(IEnumerable<MatchResponse> matches) =>
        [.. matches.Where(m => m.BracketPosition is not null).Select(m => m.BracketPosition).Order()];

    private static List<string> Players(MatchResponse match) =>
        [.. match.MatchParticipants.Select(TestTournament.NameOf).Order()];
}
