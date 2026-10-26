using System.Linq.Expressions;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class MatchParticipantProjections
{
    public static Expression<Func<MatchParticipant, GetMatchParticipantDto>> ToGetDto() =>
        mp => new GetMatchParticipantDto
        {
            Id = mp.Id,
            Username = mp.TournamentParticipant!.ApplicationUser!.UserName ?? string.Empty,
            ScoreSum = mp.ScoreSum,
            PointsSum = mp.PointsSum,
        };
}
