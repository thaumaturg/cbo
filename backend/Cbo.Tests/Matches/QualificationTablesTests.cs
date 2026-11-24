using System.Collections.Generic;
using System.Linq;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Tests.Matches;

public class QualificationTablesTests
{
    public static TheoryData<int> SupportedSizes => new(Limits.Tournaments.SupportedSizes);

    public static TheoryData<int> TableSizes => new(MatchGenerationService.s_qualificationTables.Keys);

    [Theory]
    [MemberData(nameof(SupportedSizes))]
    public void Every_supported_size_has_a_qualification_table(int size)
    {
        Assert.True(MatchGenerationService.s_qualificationTables.ContainsKey(size), $"No qualification table for {size} players.");
    }

    [Theory]
    [MemberData(nameof(TableSizes))]
    public void Every_qualification_table_is_for_a_supported_size(int size)
    {
        Assert.Contains(size, Limits.Tournaments.SupportedSizes);
    }

    [Theory]
    [MemberData(nameof(TableSizes))]
    public void Every_player_position_plays_the_same_number_of_matches(int size)
    {
        int[][] table = MatchGenerationService.s_qualificationTables[size];

        Dictionary<int, int> matchesPerPosition = table
            .SelectMany(match => match)
            .GroupBy(position => position)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(Enumerable.Range(0, size), matchesPerPosition.Keys.Order());
        Assert.Single(matchesPerPosition.Values.Distinct());
    }

    [Theory]
    [MemberData(nameof(TableSizes))]
    public void No_match_repeats_a_player(int size)
    {
        int[][] table = MatchGenerationService.s_qualificationTables[size];

        Assert.All(table, match => Assert.Equal(match.Length, match.Distinct().Count()));
    }

    [Theory]
    [MemberData(nameof(TableSizes))]
    public void Every_match_has_an_allowed_number_of_participants(int size)
    {
        int[][] table = MatchGenerationService.s_qualificationTables[size];

        Assert.All(table, match => Assert.InRange(match.Length, Limits.Matches.ParticipantsMin, Limits.Matches.ParticipantsMax));
    }
}
