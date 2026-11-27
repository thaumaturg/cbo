using System.Linq.Expressions;
using Cbo.Core.Domain;
using Cbo.Core.Tournaments;

namespace Cbo.Core.Matches;

/// <inheritdoc cref="TournamentProjections"/>
public static class MatchParticipantProjections
{
    public static Expression<Func<MatchParticipant, MatchParticipantResponse>> ToResponse() =>
        mp => new MatchParticipantResponse
        {
            Id = mp.Id,
            TournamentParticipantId = mp.TournamentParticipantId,
            Username = mp.TournamentParticipant!.ApplicationUser!.UserName ?? string.Empty,
            ScoreSum = mp.ScoreSum,
            PositiveScoreSum = mp.PositiveScoreSum,
            PointsSum = mp.PointsSum,
            Outcome = mp.Outcome,
        };
}
