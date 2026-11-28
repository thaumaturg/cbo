using Cbo.Core.Domain;

namespace Cbo.Core.Matches;

/// <summary>
/// What a player's answers add up to: their score, and the positive score that ignores what they lost.
/// </summary>
public readonly record struct AnswerScores(int Score, int PositiveScore);

public static class AnswerScoring
{
    /// <summary>
    /// Sums the answers: an accepted answer adds its question's cost, a rejected one subtracts it, and an override cost
    /// is added as is. The positive score counts only accepted answers and override costs above zero. Every marked
    /// answer needs its <see cref="RoundAnswer.Question"/>.
    /// </summary>
    public static AnswerScores Sum(IEnumerable<RoundAnswer> answers)
    {
        int score = 0;
        int positiveScore = 0;

        foreach (RoundAnswer answer in answers)
        {
            if (answer.OverrideCost.HasValue)
            {
                score += answer.OverrideCost.Value;
                positiveScore += Math.Max(answer.OverrideCost.Value, 0);
            }
            else if (answer.IsAnswerAccepted.HasValue)
            {
                Question question = answer.Question
                    ?? throw new InvalidOperationException($"The question of answer {answer.Id} isn't loaded.");

                if (answer.IsAnswerAccepted.Value)
                {
                    score += question.Cost;
                    positiveScore += question.Cost;
                }
                else
                {
                    score -= question.Cost;
                }
            }
        }

        return new AnswerScores(score, positiveScore);
    }
}
