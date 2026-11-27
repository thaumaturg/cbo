using System.Collections.Immutable;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Core.Playoffs;

/// <summary>Settles the Final into places 1 to 4, through shootouts wherever scores are equal.</summary>
public static class FinalSettlement
{
    private static readonly ImmutableArray<Outcome> s_places =
        [Outcome.FirstPlace, Outcome.SecondPlace, Outcome.ThirdPlace, Outcome.FourthPlace];

    /// <summary>
    /// Settles the Final from its own scores and those of its shootouts so far, in play order. Players with equal
    /// scores play a shootout for the places they cover, and only those still tied after it replay. Score tie-breaks
    /// are never used. Shootouts go one at a time, the group covering the lowest places first.
    /// </summary>
    /// <param name="qualificationPlaceOf">Only orders the players of the next shootout.</param>
    /// <exception cref="ArgumentException">
    /// The Final doesn't have four players, a shootout isn't played by exactly the group it is next for, or there are
    /// shootouts after the Final was settled.
    /// </exception>
    public static Settlement<TPlayer> Settle<TPlayer>(
        IReadOnlyDictionary<TPlayer, AnswerScores> matchScores,
        IReadOnlyList<IReadOnlyDictionary<TPlayer, AnswerScores>> shootoutScores,
        Func<TPlayer, int> qualificationPlaceOf)
        where TPlayer : notnull
    {
        if (matchScores.Count != s_places.Length)
            throw new ArgumentException($"The Final needs {s_places.Length} players.", nameof(matchScores));

        // Groups of players with equal scores, from the 1st place down.
        List<List<TPlayer>> groups = GroupByScore(matchScores.Keys, matchScores);

        foreach (IReadOnlyDictionary<TPlayer, AnswerScores> scores in shootoutScores)
        {
            int next = groups.FindLastIndex(group => group.Count > 1);
            if (next < 0)
                throw new ArgumentException("The Final was settled before its last shootout.", nameof(shootoutScores));

            if (!scores.Keys.ToHashSet().SetEquals(groups[next]))
            {
                throw new ArgumentException(
                    "A shootout must be played by exactly the next tied group.", nameof(shootoutScores));
            }

            List<List<TPlayer>> settled = GroupByScore(groups[next], scores);
            groups.RemoveAt(next);
            groups.InsertRange(next, settled);
        }

        int tied = groups.FindLastIndex(group => group.Count > 1);
        if (tied >= 0)
        {
            int firstPlace = groups.Take(tied).Sum(group => group.Count);
            return new ShootoutNeeded<TPlayer>(
                [.. groups[tied].OrderBy(qualificationPlaceOf)],
                s_places.Slice(firstPlace, groups[tied].Count));
        }

        return new Decided<TPlayer>(groups
            .Select((group, place) => (Player: group.Single(), Outcome: s_places[place]))
            .ToImmutableDictionary(entry => entry.Player, entry => entry.Outcome));
    }

    private static List<List<TPlayer>> GroupByScore<TPlayer>(
        IEnumerable<TPlayer> players, IReadOnlyDictionary<TPlayer, AnswerScores> scores)
        where TPlayer : notnull =>
        [.. players
            .GroupBy(player => scores[player].Score)
            .OrderByDescending(group => group.Key)
            .Select(group => group.ToList())];
}
