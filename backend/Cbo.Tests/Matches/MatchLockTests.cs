using System;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Tests.Matches;

public class MatchLockTests
{
    private static readonly DateTime ConfirmedAt = new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(TournamentStage.Qualifications)]
    [InlineData(TournamentStage.Preparations)]
    public void A_qualification_match_is_open_until_the_playoff_starts(TournamentStage stage)
    {
        Assert.Null(MatchLock.ReasonFor(MatchType.Qualification, confirmedAt: null, stage));
    }

    [Theory]
    [InlineData(TournamentStage.Playoff)]
    [InlineData(TournamentStage.Ended)]
    public void A_qualification_match_is_locked_once_the_playoff_has_started(TournamentStage stage)
    {
        Assert.Equal(MatchLockReason.PlayoffStarted, MatchLock.ReasonFor(MatchType.Qualification, confirmedAt: null, stage));
    }

    [Theory]
    [InlineData(MatchType.Playoff)]
    [InlineData(MatchType.Shootout)]
    [InlineData(MatchType.Final)]
    public void An_unconfirmed_playoff_match_shootout_or_final_is_open(MatchType type)
    {
        Assert.Null(MatchLock.ReasonFor(type, confirmedAt: null, TournamentStage.Playoff));
    }

    [Theory]
    [InlineData(MatchType.Playoff, TournamentStage.Playoff)]
    [InlineData(MatchType.Shootout, TournamentStage.Playoff)]
    [InlineData(MatchType.Final, TournamentStage.Playoff)]
    [InlineData(MatchType.Final, TournamentStage.Ended)]
    public void A_confirmed_match_is_locked(MatchType type, TournamentStage stage)
    {
        Assert.Equal(MatchLockReason.Confirmed, MatchLock.ReasonFor(type, ConfirmedAt, stage));
    }

    [Fact]
    public void Is_locked_whenever_there_is_a_reason()
    {
        Assert.True(MatchLock.IsLocked(MatchType.Playoff, ConfirmedAt, TournamentStage.Playoff));
        Assert.True(MatchLock.IsLocked(MatchType.Qualification, confirmedAt: null, TournamentStage.Playoff));
        Assert.False(MatchLock.IsLocked(MatchType.Qualification, confirmedAt: null, TournamentStage.Qualifications));
    }
}
