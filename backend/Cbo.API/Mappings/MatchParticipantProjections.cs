using System.Linq.Expressions;
using Cbo.API.Contracts.Matches;
using Cbo.API.Models.Domain;

namespace Cbo.API.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class MatchParticipantProjections
{
    public static Expression<Func<MatchParticipant, MatchParticipantResponse>> ToResponse() =>
        mp => new MatchParticipantResponse
        {
            Id = mp.Id,
            Username = mp.TournamentParticipant!.ApplicationUser!.UserName ?? string.Empty,
            ScoreSum = mp.ScoreSum,
            PointsSum = mp.PointsSum,
        };
}
