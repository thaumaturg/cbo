using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;

namespace Cbo.API.Services;

public class MatchGenerationService : IMatchGenerationService
{
    private static readonly int[][] QualificationDistribution14Players =
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
    ];

    public List<Match> GenerateQualificationMatches(Tournament tournament, List<TournamentParticipant> players)
    {
        if (players.Count != DefaultSettings.PlayersPerTournament)
        {
            throw new ArgumentException(
                $"Qualification match generation requires exactly {DefaultSettings.PlayersPerTournament} players. Got {players.Count}.",
                nameof(players));
        }

        var matches = new List<Match>();

        for (int matchIndex = 0; matchIndex < QualificationDistribution14Players.Length; matchIndex++)
        {
            int[] playerPositions = QualificationDistribution14Players[matchIndex];
            int matchNumber = matchIndex + 1;

            var match = new Match
            {
                NumberInTournament = matchNumber,
                NumberInStage = matchNumber,
                CreatedOnStage = TournamentStage.Qualifications,
                Type = Models.Constants.MatchType.Qualification,
                TournamentId = tournament.Id
            };

            foreach (int playerIndex in playerPositions)
            {
                TournamentParticipant player = players[playerIndex];

                var matchParticipant = new MatchParticipant
                {
                    TournamentParticipantId = player.Id
                };

                match.MatchParticipants.Add(matchParticipant);
            }

            matches.Add(match);
        }

        return matches;
    }
}
