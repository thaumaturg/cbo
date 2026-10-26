using System.Linq.Expressions;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class TournamentTopicProjections
{
    public static Expression<Func<TournamentTopic, GetTournamentTopicDto>> ToGetDto() =>
        tt => new GetTournamentTopicDto
        {
            Id = tt.Id,
            PriorityIndex = tt.PriorityIndex,
            TournamentId = tt.TournamentId,
            TopicId = tt.TopicId,
            TopicTitle = tt.Topic!.Title,
            TournamentParticipantId = tt.TournamentParticipantId,
            ParticipantUsername = tt.TournamentParticipant!.ApplicationUser!.UserName ?? string.Empty,
        };
}
