using System.Collections.Immutable;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Core.Playoffs;

/// <summary>Settles a playoff match into its two ordered winners and its losers, through shootouts if tied.</summary>
public static class PlayoffMatchSettlement
{
    private static readonly ImmutableArray<Outcome> s_winnerPlaces = [Outcome.FirstWinner, Outcome.SecondWinner];

    /// <summary>
    /// Settles the match from its own scores and those of its shootouts so far, in play order. There is a tie when
    /// the scores either side of the last open winner place are equal: everyone with that score plays a shootout,
    /// those above win and those below lose. A winner settled earlier ranks higher; winners settled by the same
    /// scores are ordered by score, then positive score, then qualification place.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The match has too few players, a shootout isn't played by exactly the players still tied, or there are
    /// shootouts after the match was settled.
    /// </exception>
    public static Settlement<TPlayer> Settle<TPlayer>(
        IReadOnlyDictionary<TPlayer, AnswerScores> matchScores,
        IReadOnlyList<IReadOnlyDictionary<TPlayer, AnswerScores>> shootoutScores,
        Func<TPlayer, int> qualificationPlaceOf)
        where TPlayer : notnull
    {
        if (matchScores.Count <= s_winnerPlaces.Length)
        {
            throw new ArgumentException(
                $"A playoff match needs more than {s_winnerPlaces.Length} players.", nameof(matchScores));
        }

        List<TPlayer> winners = [];
        List<TPlayer> losers = [];
        List<TPlayer>? tied = [.. matchScores.Keys];

        foreach (IReadOnlyDictionary<TPlayer, AnswerScores> scores in shootoutScores.Prepend(matchScores))
        {
            if (tied is null)
                throw new ArgumentException("The match was settled before its last shootout.", nameof(shootoutScores));

            if (!scores.Keys.ToHashSet().SetEquals(tied))
            {
                throw new ArgumentException(
                    "A shootout must be played by exactly the players still tied.", nameof(shootoutScores));
            }

            int openPlaces = s_winnerPlaces.Length - winners.Count;
            List<TPlayer> ranked = [.. tied
                .OrderByDescending(player => scores[player].Score)
                .ThenByDescending(player => scores[player].PositiveScore)
                .ThenBy(qualificationPlaceOf)];

            int lastOpenScore = scores[ranked[openPlaces - 1]].Score;
            int firstClosedScore = scores[ranked[openPlaces]].Score;

            if (lastOpenScore != firstClosedScore)
            {
                winners.AddRange(ranked.Take(openPlaces));
                losers.AddRange(ranked.Skip(openPlaces));
                tied = null;
                continue;
            }

            winners.AddRange(ranked.Where(player => scores[player].Score > lastOpenScore));
            losers.AddRange(ranked.Where(player => scores[player].Score < lastOpenScore));
            tied = [.. ranked.Where(player => scores[player].Score == lastOpenScore)];
        }

        if (tied is not null)
        {
            return new ShootoutNeeded<TPlayer>(
                [.. tied.OrderBy(qualificationPlaceOf)],
                s_winnerPlaces[winners.Count..]);
        }

        ImmutableDictionary<TPlayer, Outcome>.Builder outcomes = ImmutableDictionary.CreateBuilder<TPlayer, Outcome>();
        foreach ((TPlayer winner, Outcome place) in winners.Zip(s_winnerPlaces))
            outcomes.Add(winner, place);
        foreach (TPlayer loser in losers)
            outcomes.Add(loser, Outcome.Loser);

        return new Decided<TPlayer>(outcomes.ToImmutable());
    }
}
