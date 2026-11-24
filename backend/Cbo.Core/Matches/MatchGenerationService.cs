using System.Collections.Frozen;
using Cbo.Core.Domain;

namespace Cbo.Core.Matches;

public interface IMatchGenerationService
{
    List<Match> GenerateQualificationMatches(Tournament tournament, List<TournamentParticipant> players);
}

public class MatchGenerationService : IMatchGenerationService
{
    /// <summary>
    /// Qualification matches by tournament size. Each match lists the seeded positions of its players. Every size in
    /// <see cref="Limits.Tournaments.SupportedSizes"/> needs a table here.
    /// </summary>
    internal static readonly FrozenDictionary<int, int[][]> s_qualificationTables = new Dictionary<int, int[][]>
    {
        [14] =
        [
            [0, 7, 11, 12],
            [5, 8, 10, 13],
            [1, 2, 4, 11],
            [0, 5, 6, 9],
            [3, 6, 10, 12],
            [2, 7, 8, 9],
            [1, 3, 7, 13],
            [0, 4, 12, 13],
            [2, 5, 10, 11],
            [1, 6, 8],
            [3, 4, 9]
        ],
    }.ToFrozenDictionary();

    public List<Match> GenerateQualificationMatches(Tournament tournament, List<TournamentParticipant> players)
    {
        if (!s_qualificationTables.TryGetValue(players.Count, out int[][]? table))
        {
            throw new ArgumentException(
                $"No qualification table for {players.Count} players.",
                nameof(players));
        }

        var matches = new List<Match>();

        for (int matchIndex = 0; matchIndex < table.Length; matchIndex++)
        {
            int[] playerPositions = table[matchIndex];
            int matchNumber = matchIndex + 1;

            var match = new Match
            {
                NumberInTournament = matchNumber,
                NumberInStage = matchNumber,
                CreatedOnStage = TournamentStage.Qualifications,
                Type = MatchType.Qualification,
                TournamentId = tournament.Id
            };

            foreach (int playerIndex in playerPositions)
            {
                TournamentParticipant player = players[playerIndex];

                var matchParticipant = new MatchParticipant
                {
                    TournamentParticipantId = player.Id,
                    MatchId = Guid.Empty
                };

                match.MatchParticipants.Add(matchParticipant);
            }

            matches.Add(match);
        }

        return matches;
    }
}
