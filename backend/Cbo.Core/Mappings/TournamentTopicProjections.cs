using System.Linq.Expressions;
using Cbo.Core.Contracts.TournamentTopics;
using Cbo.Core.Models.Domain;

namespace Cbo.Core.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class TournamentTopicProjections
{
    public static Expression<Func<TournamentTopic, TournamentTopicResponse>> ToResponse() =>
        tt => new TournamentTopicResponse
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
