using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Participants;
using Cbo.Core.Playoffs;

namespace Cbo.Tests.Playoffs;

/// <summary>Replays a whole Playoff without ties, from the qualification results to the Final's places.</summary>
public class PlayoffReplayTests
{
    private static readonly (string Name, decimal Points, int Score)[] s_qualification =
    [
        ("Alex", 7m, 220),
        ("Andrew", 1.5m, -70),
        ("Gucci", 4m, 200),
        ("Dima", 2m, -10),
        ("Egor", 6m, 210),
        ("Ivanna", 8m, 220),
        ("Mihail", 4.5m, 170),
        ("Anastasia", 2m, 0),
        ("Victoria", 4.5m, 40),
        ("Nika", 6.5m, 100),
        ("Roman", 7m, 280),
        ("Simona", 1m, -120),
    ];

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

    private static readonly Dictionary<BracketPosition, string[]> s_advancing = new()
    {
        [BracketPosition.Playoff1] = ["Victoria", "Dima"],
        [BracketPosition.Playoff2] = ["Anastasia", "Mihail"],
        [BracketPosition.Playoff3] = ["Ivanna", "Victoria"],
        [BracketPosition.Playoff4] = ["Mihail", "Dima"],
        [BracketPosition.Playoff5] = ["Egor", "Andrew"],
        [BracketPosition.Playoff6] = ["Roman", "Alex"],
        [BracketPosition.Playoff7] = ["Egor", "Roman"],
        [BracketPosition.Playoff8] = ["Ivanna", "Mihail"],
        [BracketPosition.Playoff9] = ["Roman", "Victoria"],
    };

    private static List<string> PlayersInPlaceOrder() =>
        [.. QualificationPlaces
            .InPlaceOrder(
                s_qualification.Select((player, seed) => (player.Name, Result: new QualificationResult(player.Points, player.Score, 0, seed + 1))),
                player => player.Result)
            .Select(player => player.Name)];

    [Fact]
    public void The_qualification_places_match_the_example()
    {
        Assert.Equal(
            ["Ivanna", "Roman", "Alex", "Nika", "Egor", "Mihail", "Victoria", "Gucci", "Anastasia", "Dima", "Andrew", "Simona"],
            PlayersInPlaceOrder());
    }

    [Fact]
    public void The_example_plays_through_to_the_final()
    {
        List<string> playersInPlaceOrder = PlayersInPlaceOrder();
        PlayoffBracket bracket = PlayoffBracket.For(TournamentFormat.TwelveShort)!;
        Dictionary<BracketPosition, IReadOnlyDictionary<string, Outcome>> decided = [];
        List<BracketPosition> created = [];

        while (true)
        {
            ImmutableArray<NextMatch<string>> next = bracket.NextMatches(playersInPlaceOrder, decided, created);
            if (next.IsEmpty)
                break;

            foreach (NextMatch<string> match in next)
            {
                created.Add(match.Position);
                Dictionary<string, int> scores = s_scores[match.Position];
                int QualificationPlaceOf(string player) => playersInPlaceOrder.IndexOf(player) + 1;
                Assert.Equal(scores.Keys.Order(), match.Players.Order());

                Settlement<string> settlement = match.Position == BracketPosition.Final
                    ? FinalSettlement.Settle(Results(scores), [], QualificationPlaceOf)
                    : PlayoffMatchSettlement.Settle(Results(scores), [], QualificationPlaceOf);

                decided[match.Position] = Assert.IsType<Decided<string>>(settlement).Outcomes;
            }
        }

        Assert.Equal(Enum.GetValues<BracketPosition>(), created.Order());

        foreach ((BracketPosition position, string[] advancing) in s_advancing)
        {
            Assert.Equal(Outcome.FirstWinner, decided[position][advancing[0]]);
            Assert.Equal(Outcome.SecondWinner, decided[position][advancing[1]]);
        }

        Assert.Equal(Outcome.FirstPlace, decided[BracketPosition.Final]["Victoria"]);
        Assert.Equal(Outcome.SecondPlace, decided[BracketPosition.Final]["Ivanna"]);
        Assert.Equal(Outcome.ThirdPlace, decided[BracketPosition.Final]["Mihail"]);
        Assert.Equal(Outcome.FourthPlace, decided[BracketPosition.Final]["Roman"]);
    }

    private static Dictionary<string, AnswerScores> Results(Dictionary<string, int> scores) =>
        PlayoffScores.Of([.. scores.Select(entry => (entry.Key, entry.Value))]);
}
