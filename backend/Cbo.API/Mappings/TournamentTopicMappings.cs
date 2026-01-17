using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public static class TournamentTopicMappings
{
    /// <summary>
    /// Maps a TournamentTopic to GetTournamentTopicDto.
    /// Requires: Topic and TournamentParticipant.ApplicationUser to be loaded.
    /// </summary>
    public static GetTournamentTopicDto ToGetDto(this TournamentTopic tournamentTopic)
    {
        return new GetTournamentTopicDto
        {
            Id = tournamentTopic.Id,
            PriorityIndex = tournamentTopic.PriorityIndex,
            TournamentId = tournamentTopic.TournamentId,
            TopicId = tournamentTopic.TopicId,
            TopicTitle = tournamentTopic.Topic.Title,
            TournamentParticipantId = tournamentTopic.TournamentParticipantId,
            ParticipantUsername = tournamentTopic.TournamentParticipant.ApplicationUser?.UserName ?? string.Empty
        };
    }

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

    /// <summary>
    /// Creates a new TournamentTopic entity from DTO.
    /// </summary>
    public static TournamentTopic ToNewTournamentTopic(
        this UpdateTournamentTopicDto dto,
        Guid tournamentId,
        Guid tournamentParticipantId)
    {
        return new TournamentTopic
        {
            PriorityIndex = dto.PriorityIndex,
            TournamentId = tournamentId,
            TopicId = dto.TopicId,
            TournamentParticipantId = tournamentParticipantId
        };
    }
}
