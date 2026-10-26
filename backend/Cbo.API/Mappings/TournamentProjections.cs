using System.Linq.Expressions;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

/// <summary>
/// Projections that EF translates into SQL. They must stay expressions: a compiled method would make EF load
/// whole entities and run it in memory, where unloaded navigations are silently empty.
/// </summary>
public static class TournamentProjections
{
    public static Expression<Func<Tournament, GetTournamentDto>> ToGetDto(Guid currentUserId) =>
        t => new GetTournamentDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            CurrentStage = t.CurrentStage.ToString(),
            CreatedAt = t.CreatedAt,
            StartedAt = t.StartedAt,
            EndedAt = t.EndedAt,
            PlayersPerTournament = t.PlayersPerTournament,
            TopicsPerParticipantMax = t.TopicsPerParticipantMax,
            TopicsPerParticipantMin = t.TopicsPerParticipantMin,
            CurrentUserRole = t.TournamentParticipants
                .Where(p => p.ApplicationUserId == currentUserId)
                .Select(p => (TournamentParticipantRole?)p.Role)
                .FirstOrDefault(),
        };
}
