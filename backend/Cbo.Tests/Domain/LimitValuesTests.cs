using Cbo.Core.Domain;
using Cbo.Core.Paging;

namespace Cbo.Tests.Domain;

/// <summary>
/// Pins every limit and default to its value, so changing one takes a deliberate edit here too. The only tests that may
/// spell out limit values.
/// </summary>
public class LimitValuesTests
{
    [Fact]
    public void Topic_limits_have_their_agreed_values()
    {
        Assert.Equal(100, Limits.Topics.TitleLengthMax);
        Assert.Equal(500, Limits.Topics.DescriptionLengthMax);
        Assert.Equal(4000, Limits.Topics.QuestionTextLengthMax);
        Assert.Equal(4000, Limits.Topics.AnswerLengthMax);
        Assert.Equal(4000, Limits.Topics.CommentLengthMax);
        Assert.Equal(1, Limits.Topics.QuestionsMin);
        Assert.Equal(10, Limits.Topics.QuestionsMax);
        Assert.Equal(1, Limits.Topics.QuestionCostMin);
        Assert.Equal(150, Limits.Topics.QuestionCostMax);
        Assert.Equal(150, Limits.Topics.QuestionsCostTotalMax);
        Assert.Equal(10, Limits.Topics.CoAuthorsMax);
        Assert.Equal(1000, Limits.Topics.OwnedMax);
        Assert.Equal(50, Limits.Topics.ImportMax);
    }

    [Fact]
    public void Tournament_limits_have_their_agreed_values()
    {
        Assert.Equal([TournamentFormat.TwelveShort, TournamentFormat.TwelveLong], Limits.Tournaments.SelectableFormats);
        Assert.Equal(3, Limits.Tournaments.TitleLengthMin);
        Assert.Equal(100, Limits.Tournaments.TitleLengthMax);
        Assert.Equal(500, Limits.Tournaments.DescriptionLengthMax);
        Assert.Equal(0, Limits.Tournaments.TopicsPerParticipantFloor);
        Assert.Equal(50, Limits.Tournaments.TopicsPerParticipantCeiling);
    }

    [Fact]
    public void Match_limits_have_their_agreed_values()
    {
        Assert.Equal(2, Limits.Matches.ParticipantsMin);
        Assert.Equal(4, Limits.Matches.ParticipantsMax);
        Assert.Equal(4, Limits.Matches.RoundsPerQualificationMatch);
    }

    [Fact]
    public void Round_limits_have_their_agreed_values()
    {
        Assert.Equal(-9999, Limits.Rounds.OverrideCostMin);
        Assert.Equal(9999, Limits.Rounds.OverrideCostMax);
    }

    [Fact]
    public void Account_limits_have_their_agreed_values()
    {
        Assert.Equal(3, Limits.Accounts.UsernameLengthMin);
        Assert.Equal(16, Limits.Accounts.UsernameLengthMax);
        Assert.Equal(8, Limits.Accounts.PasswordLengthMin);
        Assert.Equal(64, Limits.Accounts.PasswordLengthMax);
        Assert.Equal(2, Limits.Accounts.FullNameLengthMin);
        Assert.Equal(64, Limits.Accounts.FullNameLengthMax);
        Assert.Equal(256, Limits.Accounts.EmailLengthMax);
        Assert.Equal(
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_",
            Limits.Accounts.UsernameAllowedCharacters);
        Assert.Equal(
            @"^[A-Za-z\u0100-\u017E\u0401\u0410-\u044F\u0451 '\u2019-]+$",
            Limits.Accounts.FullNamePattern);
    }

    [Fact]
    public void Tournament_defaults_have_their_agreed_values()
    {
        Assert.Equal(TournamentFormat.TwelveShort, TournamentDefaults.Format);
        Assert.Equal(6, TournamentDefaults.TopicsPerParticipantMin);
        Assert.Equal(10, TournamentDefaults.TopicsPerParticipantMax);
    }

    [Fact]
    public void Paging_defaults_have_their_agreed_values()
    {
        Assert.Equal(1, PagingDefaults.FirstPage);
        Assert.Equal(25, PagingDefaults.DefaultPageSize);
        Assert.Equal(100, PagingDefaults.MaxPageSize);
        Assert.Equal(200, PagingDefaults.MaxSearchLength);
    }
}
