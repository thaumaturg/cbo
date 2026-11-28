using System;
using System.Collections.Generic;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Playoffs;

namespace Cbo.Tests.Playoffs;

public class PlayoffMatchSettlementTests
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
        PlayoffMatchSettlement.Settle(matchScores, shootouts, player => s_qualificationPlaces[player]);

    [Fact]
    public void Without_a_tie_the_two_best_scores_win_and_the_rest_lose()
    {
        Settlement<string> settlement = Settle(PlayoffScores.Of(("A", 10), ("B", 50), ("C", 30), ("D", -20)));

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstWinner, decided.Outcomes["B"]);
        Assert.Equal(Outcome.SecondWinner, decided.Outcomes["C"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["A"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["D"]);
    }

    [Fact]
    public void Two_players_tied_for_the_last_winner_place_play_a_shootout_for_one_place()
    {
        Settlement<string> settlement = Settle(PlayoffScores.Of(("A", 50), ("B", 30), ("C", 30), ("D", 10)));

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["B", "C"], shootout.Players);
        Assert.Equal([Outcome.SecondWinner], shootout.Places);
    }

    [Fact]
    public void The_shootout_winner_is_second_behind_the_player_who_won_the_match_outright()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 50), ("B", 30), ("C", 30), ("D", 10)),
            PlayoffScores.Of(("B", -10), ("C", 20)));

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstWinner, decided.Outcomes["A"]);
        Assert.Equal(Outcome.SecondWinner, decided.Outcomes["C"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["B"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["D"]);
    }

    [Fact]
    public void Three_players_tied_for_both_winner_places_play_a_shootout_for_two_places()
    {
        Settlement<string> settlement = Settle(PlayoffScores.Of(("A", 30), ("B", 30), ("C", 30), ("D", 10)));

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["A", "B", "C"], shootout.Players);
        Assert.Equal([Outcome.FirstWinner, Outcome.SecondWinner], shootout.Places);
    }

    [Fact]
    public void Only_the_players_still_tied_after_a_shootout_play_the_next_one()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 30), ("B", 30), ("C", 30), ("D", 10)),
            PlayoffScores.Of(("A", 10), ("B", 0), ("C", 0)));

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["B", "C"], shootout.Players);
        Assert.Equal([Outcome.SecondWinner], shootout.Places);
    }

    [Fact]
    public void A_winner_settled_by_an_earlier_shootout_ranks_above_one_settled_by_a_later_shootout()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 30), ("B", 30), ("C", 30), ("D", 10)),
            PlayoffScores.Of(("A", 10), ("B", 0), ("C", 0)),
            PlayoffScores.Of(("B", 40), ("C", 20)));

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstWinner, decided.Outcomes["A"]);
        Assert.Equal(Outcome.SecondWinner, decided.Outcomes["B"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["C"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["D"]);
    }

    [Fact]
    public void Four_tied_players_all_play_a_shootout_for_two_places()
    {
        Settlement<string> settlement = Settle(PlayoffScores.Of(("A", 30), ("B", 30), ("C", 30), ("D", 30)));

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["A", "B", "C", "D"], shootout.Players);
        Assert.Equal([Outcome.FirstWinner, Outcome.SecondWinner], shootout.Places);
    }

    [Fact]
    public void Two_winners_settled_by_the_same_shootout_are_ordered_by_its_scores()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 30), ("B", 30), ("C", 30), ("D", 30)),
            PlayoffScores.Of(("A", -10), ("B", 10), ("C", 20), ("D", 0)));

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstWinner, decided.Outcomes["C"]);
        Assert.Equal(Outcome.SecondWinner, decided.Outcomes["B"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["A"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["D"]);
    }

    [Fact]
    public void Two_winners_with_equal_scores_are_not_a_tie_and_are_ordered_by_positive_score()
    {
        Settlement<string> settlement = Settle(new Dictionary<string, AnswerScores>
        {
            ["A"] = new(Score: 50, PositiveScore: 60),
            ["B"] = new(Score: 50, PositiveScore: 90),
            ["C"] = new(Score: 20, PositiveScore: 20),
            ["D"] = new(Score: 10, PositiveScore: 10),
        });

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstWinner, decided.Outcomes["B"]);
        Assert.Equal(Outcome.SecondWinner, decided.Outcomes["A"]);
    }

    [Fact]
    public void Two_winners_with_equal_score_and_positive_score_are_ordered_by_qualification_place()
    {
        Settlement<string> settlement = Settle(PlayoffScores.Of(("D", 50), ("B", 50), ("C", 20), ("A", 10)));

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstWinner, decided.Outcomes["B"]);
        Assert.Equal(Outcome.SecondWinner, decided.Outcomes["D"]);
    }

    [Fact]
    public void Two_losers_with_equal_scores_are_not_a_tie()
    {
        Settlement<string> settlement = Settle(PlayoffScores.Of(("A", 50), ("B", 30), ("C", 10), ("D", 10)));

        Decided<string> decided = Assert.IsType<Decided<string>>(settlement);
        Assert.Equal(Outcome.FirstWinner, decided.Outcomes["A"]);
        Assert.Equal(Outcome.SecondWinner, decided.Outcomes["B"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["C"]);
        Assert.Equal(Outcome.Loser, decided.Outcomes["D"]);
    }

    [Fact]
    public void A_shootout_where_nobody_scores_calls_for_another_one_with_the_same_players()
    {
        Settlement<string> settlement = Settle(
            PlayoffScores.Of(("A", 50), ("B", 30), ("C", 30), ("D", 10)),
            PlayoffScores.Of(("B", 0), ("C", 0)));

        ShootoutNeeded<string> shootout = Assert.IsType<ShootoutNeeded<string>>(settlement);
        Assert.Equal(["B", "C"], shootout.Players);
        Assert.Equal([Outcome.SecondWinner], shootout.Places);
    }

    [Fact]
    public void A_shootout_played_by_anyone_but_the_players_still_tied_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Settle(
            PlayoffScores.Of(("A", 50), ("B", 30), ("C", 30), ("D", 10)),
            PlayoffScores.Of(("A", 0), ("C", 10))));
    }

    [Fact]
    public void A_shootout_after_the_match_was_settled_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Settle(
            PlayoffScores.Of(("A", 50), ("B", 30), ("C", 20), ("D", 10)),
            PlayoffScores.Of(("B", 0), ("C", 10))));
    }
}
