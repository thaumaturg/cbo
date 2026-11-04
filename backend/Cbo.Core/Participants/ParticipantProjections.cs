using System.Linq.Expressions;
using Cbo.Core.Models.Domain;
using Cbo.Core.Tournaments;

namespace Cbo.Core.Participants;

/// <inheritdoc cref="TournamentProjections"/>
public static class ParticipantProjections
{
    public static Expression<Func<TournamentParticipant, ParticipantResponse>> ToResponse() =>
        tp => new ParticipantResponse
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
