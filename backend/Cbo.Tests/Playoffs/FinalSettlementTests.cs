using System;
using System.Collections.Generic;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Playoffs;

namespace Cbo.Tests.Playoffs;

public class FinalSettlementTests
{
    private static readonly Dictionary<string, int> s_qualificationPlaces = new()
    {
        ["A"] = 1,
        ["B"] = 2,
        ["C"] = 3,
        ["D"] = 4,
    };

    private static Settlement<string> Settle(
        Dictionary<string, AnswerScores> matchScores, params Dictionary<string, AnswerScores>[] shootouts) =>
        FinalSettlement.Settle(matchScores, shootouts, player => s_qualificationPlaces[player]);

    [Fact]
    public void Without_equal_scores_every_place_follows_the_score()
    {
        Settlement<string> settlement = Settle(PlayoffScores.Of(("A", 60), ("B", 50), ("C", -70), ("D", 70)));

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstPlace, decided.Outcomes["D"]);
        Assert.Equal(Outcome.SecondPlace, decided.Outcomes["A"]);
        Assert.Equal(Outcome.ThirdPlace, decided.Outcomes["B"]);
        Assert.Equal(Outcome.FourthPlace, decided.Outcomes["C"]);
    }

    [Fact]
    public void Two_tied_pairs_play_for_third_and_fourth_place_first()
    {
        Settlement<string> settlement = Settle(PlayoffScores.Of(("A", 60), ("B", 60), ("C", 20), ("D", 20)));

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["C", "D"], shootout.Players);
        Assert.Equal([Outcome.ThirdPlace, Outcome.FourthPlace], shootout.Places);
    }

    [Fact]
    public void Two_tied_pairs_play_for_first_and_second_place_last()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 60), ("B", 60), ("C", 20), ("D", 20)),
            PlayoffScores.Of(("C", 0), ("D", 10)));

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["A", "B"], shootout.Players);
        Assert.Equal([Outcome.FirstPlace, Outcome.SecondPlace], shootout.Places);
    }

    [Fact]
    public void Two_tied_pairs_are_decided_once_both_shootouts_split_them()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 60), ("B", 60), ("C", 20), ("D", 20)),
            PlayoffScores.Of(("C", 0), ("D", 10)),
            PlayoffScores.Of(("A", -10), ("B", 0)));

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstPlace, decided.Outcomes["B"]);
        Assert.Equal(Outcome.SecondPlace, decided.Outcomes["A"]);
        Assert.Equal(Outcome.ThirdPlace, decided.Outcomes["D"]);
        Assert.Equal(Outcome.FourthPlace, decided.Outcomes["C"]);
    }

    [Fact]
    public void Equal_scores_go_to_a_shootout_even_with_different_positive_scores()
    {
        Settlement<string> settlement = Settle(new Dictionary<string, AnswerScores>
        {
            ["A"] = new(Score: 60, PositiveScore: 60),
            ["B"] = new(Score: 40, PositiveScore: 90),
            ["C"] = new(Score: 40, PositiveScore: 40),
            ["D"] = new(Score: 10, PositiveScore: 10),
        });

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["B", "C"], shootout.Players);
        Assert.Equal([Outcome.SecondPlace, Outcome.ThirdPlace], shootout.Places);
    }

    [Fact]
    public void A_three_way_tie_settles_whoever_breaks_away_and_replays_the_rest()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 60), ("B", 20), ("C", 20), ("D", 20)),
            PlayoffScores.Of(("B", 0), ("C", 10), ("D", 0)));

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["B", "D"], shootout.Players);
        Assert.Equal([Outcome.ThirdPlace, Outcome.FourthPlace], shootout.Places);
    }

    [Fact]
    public void A_three_way_tie_is_decided_after_its_replay()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 60), ("B", 20), ("C", 20), ("D", 20)),
            PlayoffScores.Of(("B", 0), ("C", 10), ("D", 0)),
            PlayoffScores.Of(("B", 20), ("D", -20)));

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstPlace, decided.Outcomes["A"]);
        Assert.Equal(Outcome.SecondPlace, decided.Outcomes["C"]);
        Assert.Equal(Outcome.ThirdPlace, decided.Outcomes["B"]);
        Assert.Equal(Outcome.FourthPlace, decided.Outcomes["D"]);
    }

    [Fact]
    public void A_shootout_where_nobody_scores_calls_for_another_one_with_the_same_players()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 60), ("B", 60), ("C", 20), ("D", 10)),
            PlayoffScores.Of(("A", 0), ("B", 0)));

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["A", "B"], shootout.Players);
        Assert.Equal([Outcome.FirstPlace, Outcome.SecondPlace], shootout.Places);
    }

    [Fact]
    public void A_shootout_for_higher_places_before_the_lower_ones_are_settled_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Settle(
            PlayoffScores.Of(("A", 60), ("B", 60), ("C", 20), ("D", 20)),
            PlayoffScores.Of(("A", 0), ("B", 10))));
    }

    [Fact]
    public void A_shootout_after_the_final_was_settled_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Settle(
            PlayoffScores.Of(("A", 60), ("B", 50), ("C", 20), ("D", 10)),
            PlayoffScores.Of(("A", 0), ("B", 10))));
    }
}
