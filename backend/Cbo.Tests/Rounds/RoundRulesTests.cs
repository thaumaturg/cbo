using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Rounds;
using Cbo.Results;

namespace Cbo.Tests.Rounds;

public class RoundRulesTests
{
    [Theory]
    [InlineData(MatchType.Qualification)]
    [InlineData(MatchType.Playoff)]
    [InlineData(MatchType.Shootout)]
    [InlineData(MatchType.Final)]
    public void Every_round_up_to_the_last_of_the_match_kind_can_be_played(MatchType type)
    {
        Assert.Null(RoundRules.CheckRoundNumber(type, 1));
        Assert.Null(RoundRules.CheckRoundNumber(type, type.RoundsToPlay()));
    }

    [Theory]
    [InlineData(MatchType.Qualification)]
    [InlineData(MatchType.Playoff)]
    [InlineData(MatchType.Shootout)]
    [InlineData(MatchType.Final)]
    public void A_round_after_the_last_of_the_match_kind_is_out_of_range(MatchType type)
    {
        int rounds = type.RoundsToPlay();

        Failure? failure = RoundRules.CheckRoundNumber(type, rounds + 1);

        Assert.NotNull(failure);
        Assert.Equal(FailureKind.Invalid, failure.Kind);
        FieldError error = Assert.Single(failure.FieldErrors);
        Assert.Equal("numberInMatch", error.Field);
        Assert.Equal($"Round number must be between 1 and {rounds} for this match.", error.Message);
    }
}
