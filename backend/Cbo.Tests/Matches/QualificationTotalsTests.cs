using System;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Tests.Matches;

public class QualificationTotalsTests
{
    private static MatchParticipant PlayedIn(MatchType type, int? score, int? positiveScore, decimal? points) => new()
    {
        TournamentParticipantId = Guid.NewGuid(),
        MatchId = Guid.NewGuid(),
        ScoreSum = score,
        PositiveScoreSum = positiveScore,
        PointsSum = points,
        Match = new Match
        {
            NumberInTournament = 1,
            NumberInStage = 1,
            CreatedOnStage = TournamentStage.Qualifications,
            Type = type,
            TournamentId = Guid.NewGuid(),
        },
    };

    [Fact]
    public void Totals_add_up_qualification_matches_only()
    {
        QualificationTotals totals = QualificationTotals.Of([
            PlayedIn(MatchType.Qualification, score: 40, positiveScore: 60, points: 3m),
            PlayedIn(MatchType.Qualification, score: -10, positiveScore: 20, points: 0.5m),
            PlayedIn(MatchType.Playoff, score: 90, positiveScore: 90, points: null),
            PlayedIn(MatchType.Shootout, score: 10, positiveScore: 10, points: null),
            PlayedIn(MatchType.Final, score: 50, positiveScore: 50, points: null),
        ]);

        Assert.Equal(new QualificationTotals(Points: 3.5m, Score: 30, PositiveScore: 80), totals);
    }
}
