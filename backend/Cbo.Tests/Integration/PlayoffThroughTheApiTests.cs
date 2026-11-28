using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Tournaments;

namespace Cbo.Tests.Integration;

/// <summary>Plays the whole example Playoff from playoffs.md through the API and ends the tournament.</summary>
public class PlayoffThroughTheApiTests(ApiFactory api)
{
    // Seeded in the example's qualification place order, and qualifications are played with no scores, so seeds
    // decide the places.
    private static readonly string[] s_playersByPlace =
        ["Ivanna", "Roman", "Alex", "Nika", "Egor", "Mihail", "Victoria", "Gucci", "Anastasia", "Dima", "Andrew", "Simona"];

    private static readonly Dictionary<BracketPosition, Dictionary<string, int>> s_scores = new()
    {
        [BracketPosition.Playoff1] = new() { ["Egor"] = -40, ["Victoria"] = 70, ["Dima"] = 40, ["Simona"] = 10 },
        [BracketPosition.Playoff2] = new() { ["Mihail"] = 30, ["Gucci"] = -40, ["Anastasia"] = 40, ["Andrew"] = -50 },
        [BracketPosition.Playoff3] = new() { ["Ivanna"] = 40, ["Nika"] = -90, ["Victoria"] = 30, ["Anastasia"] = -60 },
        [BracketPosition.Playoff4] = new() { ["Roman"] = 0, ["Alex"] = -60, ["Dima"] = 10, ["Mihail"] = 60 },
        [BracketPosition.Playoff5] = new() { ["Egor"] = 100, ["Simona"] = -20, ["Andrew"] = 50, ["Gucci"] = -130 },
        [BracketPosition.Playoff6] = new() { ["Nika"] = -50, ["Anastasia"] = 40, ["Alex"] = 60, ["Roman"] = 140 },
        [BracketPosition.Playoff7] = new() { ["Egor"] = 70, ["Andrew"] = -20, ["Roman"] = 10, ["Alex"] = -30 },
        [BracketPosition.Playoff8] = new() { ["Ivanna"] = 40, ["Victoria"] = -30, ["Mihail"] = 10, ["Dima"] = 0 },
        [BracketPosition.Playoff9] = new() { ["Victoria"] = 70, ["Dima"] = -30, ["Egor"] = 40, ["Roman"] = 80 },
        [BracketPosition.Final] = new() { ["Ivanna"] = 60, ["Mihail"] = 50, ["Roman"] = -70, ["Victoria"] = 70 },
    };

    [Fact]
    public async Task The_example_plays_through_to_the_final_and_the_tournament_ends()
    {
        TestTournament tournament = await TestTournament.CreateAsync(api, s_playersByPlace);
        await tournament.StartPlayoffWithPlacesBySeedAsync();

        HashSet<BracketPosition> played = [];
        while (await NextUnplayedAsync(tournament, played) is MatchResponse match)
        {
            BracketPosition position = match.BracketPosition!.Value;
            Assert.Equal(s_scores[position].Keys.Order(), match.MatchParticipants.Select(TestTournament.NameOf).Order());

            await tournament.PlayAndConfirmAsync(match, s_scores[position]);
            played.Add(position);
        }

        Assert.Equal(s_scores.Keys.Order(), played.Order());

        List<MatchResponse> matches = await tournament.MatchesAsync();
        MatchResponse final = matches.Single(m => m.BracketPosition == BracketPosition.Final);
        Assert.Equal(
            new Dictionary<string, Outcome?>
            {
                ["Victoria"] = Outcome.FirstPlace,
                ["Ivanna"] = Outcome.SecondPlace,
                ["Mihail"] = Outcome.ThirdPlace,
                ["Roman"] = Outcome.FourthPlace,
            },
            final.MatchParticipants.ToDictionary(TestTournament.NameOf, p => p.Outcome));
        Assert.Equal(
            new Dictionary<string, string?>
            {
                ["Ivanna"] = "1st of #8",
                ["Mihail"] = "2nd of #8",
                ["Roman"] = "1st of #9",
                ["Victoria"] = "2nd of #9",
            },
            final.MatchParticipants.ToDictionary(TestTournament.NameOf, p => p.SourceLabel));

        // The losers of #5, #6, #7 and #9, the Losers bracket
        Assert.Equal(
            ["Alex", "Anastasia", "Andrew", "Dima", "Egor", "Gucci", "Nika", "Simona"],
            matches
                .SelectMany(m => m.MatchParticipants)
                .Where(p => p.IsEliminated)
                .Select(TestTournament.NameOf)
                .Order());

        HttpResponseMessage ended = await tournament.ChangeStageAsync(TournamentStage.Ended);

        await TestTournament.Succeeded(ended);
        TournamentResponse response = await tournament.GetAsync();
        Assert.Equal(nameof(TournamentStage.Ended), response.CurrentStage);
        Assert.NotNull(response.EndedAt);
    }

    private static async Task<MatchResponse?> NextUnplayedAsync(TestTournament tournament, HashSet<BracketPosition> played) =>
        (await tournament.MatchesAsync())
            .Where(m => m.BracketPosition is BracketPosition position && !played.Contains(position))
            .OrderBy(m => m.BracketPosition)
            .FirstOrDefault();
}
