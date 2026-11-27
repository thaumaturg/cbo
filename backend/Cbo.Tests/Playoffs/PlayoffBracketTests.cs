using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cbo.Core.Domain;
using Cbo.Core.Playoffs;

namespace Cbo.Tests.Playoffs;

public class PlayoffBracketTests
{
    private static readonly PlayoffBracket s_bracket = PlayoffBracket.For(TournamentFormat.TwelveShort)!;

    // Q1 to Q12.
    private static readonly string[] s_playersInPlaceOrder =
        ["Q1", "Q2", "Q3", "Q4", "Q5", "Q6", "Q7", "Q8", "Q9", "Q10", "Q11", "Q12"];

    private static readonly Dictionary<BracketPosition, IReadOnlyDictionary<string, Outcome>> s_nothingDecided = [];

    [Fact]
    public void The_playoff_starts_with_playoff_1_and_2_from_qualification_places()
    {
        ImmutableArray<NextMatch<string>> next = s_bracket.NextMatches(s_playersInPlaceOrder, s_nothingDecided, existing: []);

        Assert.Collection(next,
            match =>
            {
                Assert.Equal(BracketPosition.Playoff1, match.Position);
                Assert.Equal(["Q5", "Q7", "Q10", "Q12"], match.Players);
            },
            match =>
            {
                Assert.Equal(BracketPosition.Playoff2, match.Position);
                Assert.Equal(["Q6", "Q8", "Q9", "Q11"], match.Players);
            });
    }

    [Fact]
    public void A_match_waits_until_every_match_it_takes_players_from_is_decided()
    {
        Dictionary<BracketPosition, IReadOnlyDictionary<string, Outcome>> decided = new()
        {
            [BracketPosition.Playoff1] = Outcomes(first: "Q7", second: "Q10", losers: ["Q5", "Q12"]),
        };

        ImmutableArray<NextMatch<string>> next = s_bracket.NextMatches(
            s_playersInPlaceOrder, decided, existing: [BracketPosition.Playoff1, BracketPosition.Playoff2]);

        Assert.Empty(next);
    }

    [Fact]
    public void Deciding_playoff_1_and_2_creates_playoff_3_4_and_5()
    {
        Dictionary<BracketPosition, IReadOnlyDictionary<string, Outcome>> decided = new()
        {
            [BracketPosition.Playoff1] = Outcomes(first: "Q7", second: "Q10", losers: ["Q12", "Q5"]),
            [BracketPosition.Playoff2] = Outcomes(first: "Q11", second: "Q6", losers: ["Q8", "Q9"]),
        };

        ImmutableArray<NextMatch<string>> next = s_bracket.NextMatches(
            s_playersInPlaceOrder, decided, existing: [BracketPosition.Playoff1, BracketPosition.Playoff2]);

        Assert.Equal(
            [
                (BracketPosition.Playoff3, ["Q1", "Q4", "Q7", "Q11"]),
                (BracketPosition.Playoff4, ["Q2", "Q3", "Q10", "Q6"]),
                (BracketPosition.Playoff5, new[] { "Q5", "Q12", "Q8", "Q9" }),
            ],
            next.Select(match => (match.Position, match.Players.ToArray())));
    }

    [Fact]
    public void A_match_that_already_exists_is_not_created_again()
    {
        Dictionary<BracketPosition, IReadOnlyDictionary<string, Outcome>> decided = new()
        {
            [BracketPosition.Playoff1] = Outcomes(first: "Q7", second: "Q10", losers: ["Q12", "Q5"]),
            [BracketPosition.Playoff2] = Outcomes(first: "Q11", second: "Q6", losers: ["Q8", "Q9"]),
        };

        ImmutableArray<NextMatch<string>> next = s_bracket.NextMatches(
            s_playersInPlaceOrder,
            decided,
            existing: [BracketPosition.Playoff1, BracketPosition.Playoff2, BracketPosition.Playoff3, BracketPosition.Playoff5]);

        Assert.Equal([BracketPosition.Playoff4], next.Select(match => match.Position));
    }

    [Theory]
    [InlineData(BracketPosition.Playoff1, new[] { BracketPosition.Playoff3, BracketPosition.Playoff4 }, BracketPosition.Playoff5)]
    [InlineData(BracketPosition.Playoff2, new[] { BracketPosition.Playoff3, BracketPosition.Playoff4 }, BracketPosition.Playoff5)]
    [InlineData(BracketPosition.Playoff3, new[] { BracketPosition.Playoff8 }, BracketPosition.Playoff6)]
    [InlineData(BracketPosition.Playoff4, new[] { BracketPosition.Playoff8 }, BracketPosition.Playoff6)]
    [InlineData(BracketPosition.Playoff5, new[] { BracketPosition.Playoff7 }, null)]
    [InlineData(BracketPosition.Playoff6, new[] { BracketPosition.Playoff7 }, null)]
    [InlineData(BracketPosition.Playoff7, new[] { BracketPosition.Playoff9 }, null)]
    [InlineData(BracketPosition.Playoff8, new[] { BracketPosition.Final }, BracketPosition.Playoff9)]
    [InlineData(BracketPosition.Playoff9, new[] { BracketPosition.Final }, null)]
    [InlineData(BracketPosition.Final, new BracketPosition[0], null)]
    public void Winners_and_losers_go_where_the_bracket_table_says(
        BracketPosition position, BracketPosition[] winners, BracketPosition? losers)
    {
        BracketDestinations destinations = s_bracket.Destinations(position);

        Assert.Equal(winners, destinations.Winners);
        Assert.Equal(losers, destinations.Losers);
    }

    [Theory]
    [InlineData(TournamentFormat.TwelveShort)]
    [InlineData(TournamentFormat.TwelveLong)]
    public void Every_qualification_place_enters_the_bracket_once(TournamentFormat format)
    {
        PlayoffBracket bracket = PlayoffBracket.For(format)!;

        IEnumerable<int> places = bracket.Matches.Values
            .SelectMany(slots => slots)
            .OfType<QualificationPlaceSlot>()
            .Select(slot => slot.Place)
            .Order();

        Assert.Equal(Enumerable.Range(1, format.TournamentSize()), places);
    }

    [Theory]
    [InlineData(TournamentFormat.TwelveShort)]
    [InlineData(TournamentFormat.TwelveLong)]
    public void Every_match_in_the_bracket_has_the_most_players_a_match_can_have(TournamentFormat format)
    {
        PlayoffBracket bracket = PlayoffBracket.For(format)!;

        Assert.All(bracket.Matches.Values, slots => Assert.Equal(Limits.Matches.ParticipantsMax, slots.Length));
    }

    [Fact]
    public void Format_14_has_no_bracket_yet()
    {
        Assert.Null(PlayoffBracket.For(TournamentFormat.Fourteen));
    }

    [Fact]
    public void Each_player_is_labelled_with_where_they_came_from()
    {
        Dictionary<BracketPosition, IReadOnlyDictionary<string, Outcome>> decided = new()
        {
            [BracketPosition.Playoff7] = Outcomes(first: "Q5", second: "Q2", losers: ["Q8", "Q11"]),
            [BracketPosition.Playoff8] = Outcomes(first: "Q1", second: "Q6", losers: ["Q7", "Q4"]),
        };

        ImmutableArray<SeatedPlayer<string>>? players = s_bracket.PlayersOf(BracketPosition.Playoff9, s_playersInPlaceOrder, decided);

        Assert.NotNull(players);
        Assert.Equal(
            [("Q4", "dropped from #8"), ("Q7", "dropped from #8"), ("Q5", "1st of #7"), ("Q2", "2nd of #7")],
            players.Value.Select(seat => (seat.Player, seat.Slot.Label)));
    }

    [Fact]
    public void Players_entering_from_qualification_are_labelled_with_their_place()
    {
        ImmutableArray<SeatedPlayer<string>>? players = s_bracket.PlayersOf(BracketPosition.Playoff2, s_playersInPlaceOrder, s_nothingDecided);

        Assert.NotNull(players);
        Assert.Equal(["Q6", "Q8", "Q9", "Q11"], players.Value.Select(seat => seat.Slot.Label));
    }

    [Fact]
    public void A_match_whose_sources_are_not_decided_has_no_players_yet()
    {
        Assert.Null(s_bracket.PlayersOf(BracketPosition.Playoff3, s_playersInPlaceOrder, s_nothingDecided));
    }

    [Fact]
    public void Reopening_playoff_1_deletes_everything_after_it_that_exists()
    {
        ReopenDeletion deletion = s_bracket.DeletedByReopening(
            BracketPosition.Playoff1,
            shootout: null,
            existingPositions:
            [
                BracketPosition.Playoff1, BracketPosition.Playoff2, BracketPosition.Playoff3, BracketPosition.Playoff4,
                BracketPosition.Playoff5, BracketPosition.Playoff6, BracketPosition.Playoff8,
            ],
            existingShootouts: []);

        Assert.Equal(
            [
                BracketPosition.Playoff3, BracketPosition.Playoff4, BracketPosition.Playoff5, BracketPosition.Playoff6,
                BracketPosition.Playoff8,
            ],
            deletion.Positions);
        Assert.Empty(deletion.Shootouts);
    }

    [Fact]
    public void Reopening_playoff_1_reaches_the_final()
    {
        ReopenDeletion deletion = s_bracket.DeletedByReopening(
            BracketPosition.Playoff1,
            shootout: null,
            existingPositions: Enum.GetValues<BracketPosition>(),
            existingShootouts: []);

        Assert.Equal(Enum.GetValues<BracketPosition>().Except([BracketPosition.Playoff1, BracketPosition.Playoff2]), deletion.Positions);
    }

    [Fact]
    public void Reopening_playoff_5_never_touches_playoff_8()
    {
        ReopenDeletion deletion = s_bracket.DeletedByReopening(
            BracketPosition.Playoff5,
            shootout: null,
            existingPositions: Enum.GetValues<BracketPosition>(),
            existingShootouts: []);

        Assert.Equal([BracketPosition.Playoff7, BracketPosition.Playoff9, BracketPosition.Final], deletion.Positions);
    }

    [Fact]
    public void Reopening_a_match_deletes_all_of_its_shootouts()
    {
        ReopenDeletion deletion = s_bracket.DeletedByReopening(
            BracketPosition.Playoff3,
            shootout: null,
            existingPositions: [BracketPosition.Playoff3, BracketPosition.Playoff4],
            existingShootouts: [1, 2]);

        Assert.Equal([1, 2], deletion.Shootouts);
        Assert.Empty(deletion.Positions);
    }

    [Fact]
    public void Reopening_a_shootout_deletes_only_the_shootouts_after_it_and_the_matches_after_its_match()
    {
        ReopenDeletion deletion = s_bracket.DeletedByReopening(
            BracketPosition.Playoff3,
            shootout: 2,
            existingPositions:
            [
                BracketPosition.Playoff1, BracketPosition.Playoff2, BracketPosition.Playoff3, BracketPosition.Playoff4,
                BracketPosition.Playoff5, BracketPosition.Playoff6, BracketPosition.Playoff8,
            ],
            existingShootouts: [1, 2, 3]);

        Assert.Equal([3], deletion.Shootouts);
        Assert.Equal([BracketPosition.Playoff6, BracketPosition.Playoff8], deletion.Positions);
    }

    private static IReadOnlyDictionary<string, Outcome> Outcomes(string first, string second, string[] losers) =>
        losers
            .ToDictionary(loser => loser, _ => Outcome.Loser)
            .Append(new(first, Outcome.FirstWinner))
            .Append(new(second, Outcome.SecondWinner))
            .ToDictionary();
}
