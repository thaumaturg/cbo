using System.Linq.Expressions;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class ParticipantProjections
{
    public static Expression<Func<TournamentParticipant, GetTournamentParticipantDto>> ToGetDto() =>
        tp => new GetTournamentParticipantDto
        {
            Id = tp.Id,
            Username = tp.ApplicationUser!.UserName ?? string.Empty,
            Role = tp.Role,
            Seed = tp.Seed,
            ScoreSum = tp.ScoreSum,
            PointsSum = tp.PointsSum,
            TopicsCount = tp.TournamentTopics.Count,
            TournamentId = tp.TournamentId,
            ApplicationUserId = tp.ApplicationUserId,
        };
}
