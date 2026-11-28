using System;
using System.Collections.Generic;
using System.Linq;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Tests.Matches;

public class QualificationTablesTests
{
    public static TheoryData<TournamentFormat> Formats => new(Enum.GetValues<TournamentFormat>());

    [Theory]
    [MemberData(nameof(Formats))]
    public void Every_format_has_a_qualification_table(TournamentFormat format)
    {
        Assert.True(MatchGenerationService.s_qualificationTables.ContainsKey(format), $"No qualification table for {format}.");
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Every_player_position_plays_the_same_number_of_matches(TournamentFormat format)
    {
        int[][] table = MatchGenerationService.s_qualificationTables[format];

        Dictionary<int, int> matchesPerPosition = table
            .SelectMany(match => match)
            .GroupBy(position => position)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(Enumerable.Range(0, format.TournamentSize()), matchesPerPosition.Keys.Order());
        Assert.Single(matchesPerPosition.Values.Distinct());
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void No_match_repeats_a_player(TournamentFormat format)
    {
        int[][] table = MatchGenerationService.s_qualificationTables[format];

        Assert.All(table, match => Assert.Equal(match.Length, match.Distinct().Count()));
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Every_match_has_an_allowed_number_of_participants(TournamentFormat format)
    {
        int[][] table = MatchGenerationService.s_qualificationTables[format];

        Assert.All(table, match => Assert.InRange(match.Length, Limits.Matches.ParticipantsMin, Limits.Matches.ParticipantsMax));
    }

    [Theory]
    [InlineData(TournamentFormat.TwelveShort, 12, 9, 3)]
    [InlineData(TournamentFormat.TwelveLong, 12, 12, 4)]
    [InlineData(TournamentFormat.Fourteen, 14, 11, 3)]
    public void Every_format_has_its_size_match_count_and_matches_per_player(
        TournamentFormat format, int size, int matchCount, int matchesPerPlayer)
    {
        int[][] table = MatchGenerationService.s_qualificationTables[format];

        Assert.Equal(size, format.TournamentSize());
        Assert.Equal(matchCount, table.Length);
        Assert.All(
            table.SelectMany(match => match).GroupBy(position => position),
            group => Assert.Equal(matchesPerPlayer, group.Count()));
    }

    // Copied from docs/specs/playoff.md (A1), where seeds are 1-based
    [Fact]
    public void The_12_short_table_matches_the_spec()
    {
        int[][] spec =
        [
            [3, 10, 4, 2],
            [8, 9, 2, 11],
            [5, 10, 2, 7],
            [10, 8, 6, 1],
            [9, 12, 1, 6],
            [5, 3, 11, 12],
            [4, 7, 8, 12],
            [4, 1, 5, 9],
            [7, 11, 6, 3],
        ];

        AssertTableMatchesSpec(spec, MatchGenerationService.s_qualificationTables[TournamentFormat.TwelveShort]);
    }

    // Copied from docs/specs/playoff.md (A1), where seeds are 1-based
    [Fact]
    public void The_12_long_table_matches_the_spec()
    {
        int[][] spec =
        [
            [1, 2, 3, 4],
            [5, 6, 7, 8],
            [1, 9, 10, 11],
            [2, 3, 5, 12],
            [4, 6, 7, 9],
            [1, 8, 10, 12],
            [2, 5, 9, 11],
            [3, 4, 6, 10],
            [1, 7, 11, 12],
            [4, 5, 8, 9],
            [2, 6, 10, 12],
            [3, 7, 8, 11],
        ];

        AssertTableMatchesSpec(spec, MatchGenerationService.s_qualificationTables[TournamentFormat.TwelveLong]);
    }

    // The order within a match has no meaning, the order of the matches does
    private static void AssertTableMatchesSpec(int[][] oneBasedSpec, int[][] table)
    {
        Assert.Equal(oneBasedSpec.Length, table.Length);
        for (int i = 0; i < table.Length; i++)
        {
            Assert.Equal(oneBasedSpec[i].Select(seed => seed - 1).Order(), table[i].Order());
        }
    }
}
