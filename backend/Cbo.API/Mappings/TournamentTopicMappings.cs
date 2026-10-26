using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public static class TournamentTopicMappings
{
    /// <summary>
    /// Maps a TournamentTopic to GetAvailableTopicDto.
    /// Requires: Topic and TournamentParticipant.ApplicationUser to be loaded.
    /// </summary>
    public static GetAvailableTopicDto ToAvailableDto(this TournamentTopic tournamentTopic)
    {
        return new GetAvailableTopicDto
        {
            PriorityIndex = tournamentTopic.PriorityIndex,
            TopicId = tournamentTopic.TopicId,
            TopicTitle = tournamentTopic.Topic.Title,
            OwnerUsername = tournamentTopic.TournamentParticipant.ApplicationUser?.UserName ?? string.Empty
        };
    }
}
