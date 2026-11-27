using System;
using System.Linq;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Tests.Matches;

public class MatchScoresTests
{
    // Every round, Alice answers a 30 correctly and Bob gets a 10 wrong
    private static Match Played(MatchType type, int rounds)
    {
        var match = new Match
        {
            Id = Guid.NewGuid(),
            NumberInTournament = 1,
            NumberInStage = 1,
            CreatedOnStage = TournamentStage.Qualifications,
            Type = type,
            TournamentId = Guid.NewGuid(),
        };
        MatchParticipant alice = Participant(match);
        MatchParticipant bob = Participant(match);
        match.MatchParticipants = [alice, bob];

        for (int number = 1; number <= rounds; number++)
        {
            var round = new Round { Id = Guid.NewGuid(), NumberInMatch = number, TopicId = Guid.NewGuid(), MatchId = match.Id };
            round.RoundAnswers = [Answer(round, alice, cost: 30, accepted: true), Answer(round, bob, cost: 10, accepted: false)];
            match.Rounds.Add(round);
        }

        return match;
    }

    private static MatchParticipant Participant(Match match) => new()
    {
        Id = Guid.NewGuid(),
        TournamentParticipantId = Guid.NewGuid(),
        MatchId = match.Id,
    };

    private static RoundAnswer Answer(Round round, MatchParticipant participant, int cost, bool accepted) => new()
    {
        RoundId = round.Id,
        QuestionId = Guid.NewGuid(),
        MatchParticipantId = participant.Id,
        IsAnswerAccepted = accepted,
        Question = new Question { QuestionNumber = 1, Cost = cost, Text = "Q", Answer = "A", TopicId = round.TopicId },
    };

    private static (MatchParticipant Alice, MatchParticipant Bob) Players(Match match) =>
        (match.MatchParticipants.First(), match.MatchParticipants.Last());

    private static void AssertScores(MatchParticipant participant, int score, int positiveScore, decimal? points)
    {
        Assert.Equal(score, participant.ScoreSum);
        Assert.Equal(positiveScore, participant.PositiveScoreSum);
        Assert.Equal(points, participant.PointsSum);
    }

    [Fact]
    public void A_finished_qualification_match_gets_scores_and_points()
    {
        int rounds = MatchType.Qualification.RoundsToPlay();
        Match match = Played(MatchType.Qualification, rounds);

        MatchScores.Recalculate(match);

        (MatchParticipant alice, MatchParticipant bob) = Players(match);
        AssertScores(alice, 30 * rounds, 30 * rounds, 4m);
        AssertScores(bob, -10 * rounds, 0, 2m);
    }

    [Fact]
    public void An_unfinished_qualification_match_gets_scores_but_no_points()
    {
        int playedRounds = MatchType.Qualification.RoundsToPlay() - 1;
        Match match = Played(MatchType.Qualification, playedRounds);
        foreach (MatchParticipant participant in match.MatchParticipants)
            participant.PointsSum = 3m;

        MatchScores.Recalculate(match);

        (MatchParticipant alice, MatchParticipant bob) = Players(match);
        AssertScores(alice, 30 * playedRounds, 30 * playedRounds, null);
        AssertScores(bob, -10 * playedRounds, 0, null);
    }

    [Theory]
    [InlineData(MatchType.Playoff)]
    [InlineData(MatchType.Shootout)]
    [InlineData(MatchType.Final)]
    public void A_finished_playoff_match_shootout_or_final_gets_scores_but_never_points(MatchType type)
    {
        int rounds = type.RoundsToPlay();
        Match match = Played(type, rounds);
        foreach (MatchParticipant participant in match.MatchParticipants)
            participant.PointsSum = 3m;

        MatchScores.Recalculate(match);

        (MatchParticipant alice, MatchParticipant bob) = Players(match);
        AssertScores(alice, 30 * rounds, 30 * rounds, null);
        AssertScores(bob, -10 * rounds, 0, null);
    }
}
