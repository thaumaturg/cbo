using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Participants;
using Cbo.Core.Tournaments;

namespace Cbo.Tests.Integration;

public class RollBackTests(ApiFactory api)
{
    private static readonly string[] s_playersBySeed = ["S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12"];

    [Fact]
    public async Task Rolling_back_to_the_playoff_wipes_only_the_end_time()
    {
        TestTournament tournament = await EndedAsync();
        List<MatchResponse> matchesBefore = await tournament.MatchesAsync();

        TournamentResponse rolledBack = await tournament.RolledBackAsync(TournamentStage.Playoff);

        Assert.Equal(nameof(TournamentStage.Playoff), rolledBack.CurrentStage);
        Assert.Null(rolledBack.EndedAt);
        Assert.NotNull(rolledBack.StartedAt);
        List<MatchResponse> matches = await tournament.MatchesAsync();
        Assert.Equal(matchesBefore.Select(m => (m.Id, m.RoundsCount, m.ConfirmedAt)), matches.Select(m => (m.Id, m.RoundsCount, m.ConfirmedAt)));
        Assert.Equal(
            matchesBefore.SelectMany(m => m.MatchParticipants).Select(p => (p.Id, p.SourceLabel, p.Outcome)),
            matches.SelectMany(m => m.MatchParticipants).Select(p => (p.Id, p.SourceLabel, p.Outcome)));
    }

    [Fact]
    public async Task Rolling_back_to_qualifications_wipes_the_playoff_and_the_end_time_and_unlocks_qualification_matches()
    {
        TestTournament tournament = await EndedAsync();
        List<MatchResponse> qualificationsBefore = [.. (await tournament.MatchesAsync()).Where(m => m.Type == MatchType.Qualification)];
        Assert.All(qualificationsBefore, m => Assert.True(m.IsLocked));
        List<ParticipantResponse> playersBefore = await PlayersAsync(tournament);

        TournamentResponse rolledBack = await tournament.RolledBackAsync(TournamentStage.Qualifications);

        Assert.Equal(nameof(TournamentStage.Qualifications), rolledBack.CurrentStage);
        Assert.Null(rolledBack.EndedAt);
        Assert.NotNull(rolledBack.StartedAt);
        List<MatchResponse> matches = await tournament.MatchesAsync();
        Assert.Equal(qualificationsBefore.Select(m => (m.Id, m.RoundsCount)), matches.Select(m => (m.Id, m.RoundsCount)));
        Assert.All(matches, m => Assert.False(m.IsLocked));
        Assert.Equal(
            playersBefore.Select(p => (p.Id, p.PointsSum, p.ScoreSum, p.PositiveScoreSum)),
            (await PlayersAsync(tournament)).Select(p => (p.Id, p.PointsSum, p.ScoreSum, p.PositiveScoreSum)));
        await TestTournament.Succeeded(await tournament.RescoreAsync(matches[0].Id, new Dictionary<string, int>()));
    }

    [Fact]
    public async Task Rolling_back_to_qualifications_wipes_shootouts_too()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();
        MatchResponse playoff1 = await tournament.MatchAsync(BracketPosition.Playoff1);
        await tournament.PlayAndConfirmAsync(playoff1, new Dictionary<string, int> { ["S5"] = 50, ["S7"] = 30, ["S10"] = 30, ["S12"] = 10 });
        Assert.Contains(await tournament.MatchesAsync(), m => m.Type == MatchType.Shootout);

        await tournament.RolledBackAsync(TournamentStage.Qualifications);

        Assert.All(await tournament.MatchesAsync(), m => Assert.Equal(MatchType.Qualification, m.Type));
    }

    [Fact]
    public async Task After_rolling_back_to_qualifications_the_places_follow_the_qualification_results_again()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();
        await tournament.RolledBackAsync(TournamentStage.Qualifications);

        // S12 wins the match it plays with S1, S6 and S9 and tops the standings; those three drop below everyone else
        MatchResponse withS12 = (await tournament.MatchesAsync()).Single(m =>
            m.MatchParticipants.Select(TestTournament.NameOf).Order().SequenceEqual(["S1", "S12", "S6", "S9"]));
        await TestTournament.Succeeded(await tournament.RescoreAsync(withS12.Id, new Dictionary<string, int> { ["S12"] = 50 }));
        await TestTournament.Succeeded(await tournament.ChangeStageAsync(TournamentStage.Playoff));

        MatchResponse playoff1 = await tournament.MatchAsync(BracketPosition.Playoff1);
        Assert.Equal(
            [("S1", "Q10"), ("S5", "Q5"), ("S8", "Q7"), ("S9", "Q12")],
            playoff1.MatchParticipants.Select(p => (TestTournament.NameOf(p), p.SourceLabel)).Order());
    }

    [Fact]
    public async Task Rolling_back_to_preparations_wipes_every_match_the_totals_and_the_start_and_end_times()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartQualificationsAsync();
        await tournament.PlayQualificationsAsync(new Dictionary<string, int> { ["S1"] = 30 });
        await TestTournament.Succeeded(await tournament.ChangeStageAsync(TournamentStage.Playoff));
        await tournament.PlayOutPlayoffAsync();
        await TestTournament.Succeeded(await tournament.ChangeStageAsync(TournamentStage.Ended));

        TournamentResponse rolledBack = await tournament.RolledBackAsync(TournamentStage.Preparations);

        Assert.Equal(nameof(TournamentStage.Preparations), rolledBack.CurrentStage);
        Assert.Null(rolledBack.StartedAt);
        Assert.Null(rolledBack.EndedAt);
        Assert.Empty(await tournament.MatchesAsync());
        List<ParticipantResponse> players = await PlayersAsync(tournament);
        Assert.Equal(s_playersBySeed, players.OrderBy(p => p.Seed).Select(p => p.Username[..p.Username.LastIndexOf('_')]));
        Assert.All(players, p => Assert.Equal((null, null, null), (p.PointsSum, p.ScoreSum, p.PositiveScoreSum)));
    }

    [Fact]
    public async Task After_rolling_back_to_preparations_the_tournament_can_be_edited_and_started_again()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartQualificationsAsync();
        await tournament.RolledBackAsync(TournamentStage.Preparations);

        HttpResponseMessage edited = await tournament.Creator.PutAsJsonAsync(
            $"/api/Tournaments/{tournament.Id}",
            new { title = "Rolled back", format = TournamentFormat.TwelveLong, topicsPerParticipantMax = TestTournament.StaffTopicCount, topicsPerParticipantMin = 0 },
            TestTournament.JsonOptions,
            TestContext.Current.CancellationToken);
        await TestTournament.Succeeded(edited);
        await tournament.StartQualificationsAsync();

        Assert.Equal(12, (await tournament.MatchesAsync()).Count);
    }

    [Fact]
    public async Task Topics_played_in_wiped_rounds_are_offered_again()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();
        MatchResponse playoff1 = await tournament.MatchAsync(BracketPosition.Playoff1);
        await tournament.PlayAsync(playoff1, playoff1.MatchParticipants.ToDictionary(TestTournament.NameOf, _ => 0));
        List<Guid> playedInPlayoff1 = [.. (await tournament.MatchAsync(playoff1.Id)).Rounds.Select(r => r.TopicId)];
        Guid qualificationMatch = (await tournament.MatchesAsync()).First(m => m.Type == MatchType.Qualification).Id;
        Assert.DoesNotContain(await AvailableTopicsAsync(tournament, qualificationMatch), playedInPlayoff1.Contains);

        await tournament.RolledBackAsync(TournamentStage.Qualifications);

        Assert.Subset((await AvailableTopicsAsync(tournament, qualificationMatch)).ToHashSet(), playedInPlayoff1.ToHashSet());
    }

    [Theory]
    [InlineData(TournamentStage.Preparations)]
    [InlineData(TournamentStage.Qualifications)]
    public async Task Rolling_back_to_the_current_stage_is_rejected(TournamentStage stage)
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        if (stage == TournamentStage.Qualifications)
            await tournament.StartQualificationsAsync();

        HttpResponseMessage response = await tournament.ChangeStageAsync(stage);

        await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Equal(stage.ToString(), (await tournament.GetAsync()).CurrentStage);
    }

    [Fact]
    public async Task Moving_to_a_later_stage_than_the_next_one_is_rejected()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartQualificationsAsync();

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Ended);

        await TestTournament.FailedWith(HttpStatusCode.Conflict, response);
        Assert.Equal(nameof(TournamentStage.Qualifications), (await tournament.GetAsync()).CurrentStage);
    }

    [Fact]
    public async Task Only_the_creator_can_roll_the_tournament_back()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartQualificationsAsync();

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Preparations, tournament.Player);

        await TestTournament.FailedWith(HttpStatusCode.Forbidden, response);
        Assert.Equal(nameof(TournamentStage.Qualifications), (await tournament.GetAsync()).CurrentStage);
    }

    private async Task<TestTournament> EndedAsync()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersBySeed);
        await tournament.StartPlayoffWithPlacesBySeedAsync();
        await tournament.PlayOutPlayoffAsync();
        await TestTournament.Succeeded(await tournament.ChangeStageAsync(TournamentStage.Ended));
        return tournament;
    }

    private static async Task<List<ParticipantResponse>> PlayersAsync(TestTournament tournament) =>
        (await tournament.Creator.GetFromJsonAsync<List<ParticipantResponse>>(
            $"/api/Tournaments/{tournament.Id}/participants?role=Player", TestTournament.JsonOptions))!;

    private static async Task<List<Guid>> AvailableTopicsAsync(TestTournament tournament, Guid matchId) =>
        [.. (await tournament.Creator.GetFromJsonAsync<List<AvailableTopicResponse>>(
            $"/api/Tournaments/{tournament.Id}/matches/{matchId}/available-topics", TestTournament.JsonOptions))!.Select(t => t.TopicId)];
}
