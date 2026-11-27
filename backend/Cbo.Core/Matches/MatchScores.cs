using Cbo.Core.Domain;

namespace Cbo.Core.Matches;

/// <summary>A match's results for each of its participants, from the answers in its rounds.</summary>
public static class MatchScores
{
    /// <summary>
    /// Sets the score and positive score of the match's participants from <see cref="Match.Rounds"/>, and their points
    /// once a qualification match is finished. Other kinds of match never get points. Every answer needs its
    /// <see cref="RoundAnswer.Question"/>.
    /// </summary>
    public static void Recalculate(Match match)
    {
        ILookup<Guid, RoundAnswer> answersByParticipant = match.Rounds
            .SelectMany(r => r.RoundAnswers)
            .ToLookup(ra => ra.MatchParticipantId);

        foreach (MatchParticipant participant in match.MatchParticipants)
        {
            AnswerScores scores = AnswerScoring.Sum(answersByParticipant[participant.Id]);
            participant.ScoreSum = scores.Score;
            participant.PositiveScoreSum = scores.PositiveScore;
        }

        if (match.Type == MatchType.Qualification && match.Type.IsFinished(match.Rounds.Count))
        {
            CalculatePoints(match.MatchParticipants.ToList());
        }
        else
        {
            foreach (MatchParticipant participant in match.MatchParticipants)
            {
                participant.PointsSum = null;
            }
        }
    }

    private static void CalculatePoints(List<MatchParticipant> participants)
    {
        int count = participants.Count;
        if (count < Limits.Matches.ParticipantsMin || count > Limits.Matches.ParticipantsMax)
            return;

        List<MatchParticipant> sorted = participants.OrderByDescending(p => p.ScoreSum ?? 0).ToList();
        decimal[] points = GetPointsDistribution(sorted);

        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].PointsSum = points[i];
        }
    }

    private static decimal[] GetPointsDistribution(List<MatchParticipant> sorted)
    {
        int count = sorted.Count;
        int[] scores = sorted.Select(p => p.ScoreSum ?? 0).ToArray();

        return count switch
        {
            2 => GetPointsFor2Players(scores),
            3 => GetPointsFor3Players(scores),
            4 => GetPointsFor4Players(scores),
            _ => []
        };
    }

    private static decimal[] GetPointsFor2Players(int[] scores)
    {
        if (scores[0] == scores[1])
            return [3m, 3m];

        return [4m, 2m];
    }

    private static decimal[] GetPointsFor3Players(int[] scores)
    {
        bool top2Same = scores[0] == scores[1];
        bool bottom2Same = scores[1] == scores[2];

        if (top2Same && bottom2Same)
            return [2m, 2m, 2m];

        if (top2Same)
            return [2.5m, 2.5m, 1m];

        if (bottom2Same)
            return [3m, 1.5m, 1.5m];

        return [3m, 2m, 1m];
    }

    // Points are split across tied places.
    // Base points: 1st = 3, 2nd = 2, 3rd = 1, 4th = 0.
    // Tied players share the sum of their places equally.
    private static decimal[] GetPointsFor4Players(int[] scores)
    {
        bool firstTiedWithSecond = scores[0] == scores[1];
        bool secondTiedWithThird = scores[1] == scores[2];
        bool thirdTiedWithFourth = scores[2] == scores[3];

        // All four tied: (3+2+1+0)/4 = 1.5 each
        if (firstTiedWithSecond && secondTiedWithThird && thirdTiedWithFourth)
            return [1.5m, 1.5m, 1.5m, 1.5m];

        if (firstTiedWithSecond && secondTiedWithThird && !thirdTiedWithFourth)
            return [2m, 2m, 2m, 0m];

        if (!firstTiedWithSecond && secondTiedWithThird && thirdTiedWithFourth)
            return [3m, 1m, 1m, 1m];

        if (firstTiedWithSecond && !secondTiedWithThird && thirdTiedWithFourth)
            return [2.5m, 2.5m, 0.5m, 0.5m];

        if (firstTiedWithSecond && !secondTiedWithThird && !thirdTiedWithFourth)
            return [2.5m, 2.5m, 1m, 0m];

        if (!firstTiedWithSecond && secondTiedWithThird && !thirdTiedWithFourth)
            return [3m, 1.5m, 1.5m, 0m];

        if (!firstTiedWithSecond && !secondTiedWithThird && thirdTiedWithFourth)
            return [3m, 2m, 0.5m, 0.5m];

        return [3m, 2m, 1m, 0m];
    }
}
