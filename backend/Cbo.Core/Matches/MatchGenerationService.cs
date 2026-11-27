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
    /// Qualification matches by format. Each match lists the seeded positions of its players, 0-based. Every
    /// <see cref="TournamentFormat"/> needs a table here.
    /// </summary>
    internal static readonly FrozenDictionary<TournamentFormat, int[][]> s_qualificationTables = new Dictionary<TournamentFormat, int[][]>
    {
        [TournamentFormat.Fourteen] =
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
        [TournamentFormat.TwelveShort] =
        [
            [2, 9, 3, 1],
            [7, 8, 1, 10],
            [4, 9, 1, 6],
            [9, 7, 5, 0],
            [8, 11, 0, 5],
            [4, 2, 10, 11],
            [3, 6, 7, 11],
            [3, 0, 4, 8],
            [6, 10, 5, 2]
        ],
        [TournamentFormat.TwelveLong] =
        [
            [0, 1, 2, 3],
            [4, 5, 6, 7],
            [0, 8, 9, 10],
            [1, 2, 4, 11],
            [3, 5, 6, 8],
            [0, 7, 9, 11],
            [1, 4, 8, 10],
            [2, 3, 5, 9],
            [0, 6, 10, 11],
            [3, 4, 7, 8],
            [1, 5, 9, 11],
            [2, 6, 7, 10]
        ],
    }.ToFrozenDictionary();

    public List<Match> GenerateQualificationMatches(Tournament tournament, List<TournamentParticipant> players)
    {
        if (!s_qualificationTables.TryGetValue(tournament.Format, out int[][]? table))
        {
            throw new ArgumentException(
                $"No qualification table for format {tournament.Format}.",
                nameof(tournament));
        }

        if (players.Count != tournament.Format.TournamentSize())
        {
            throw new ArgumentException(
                $"Format {tournament.Format} needs {tournament.Format.TournamentSize()} players, not {players.Count}.",
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
