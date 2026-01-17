using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public static class ParticipantMappings
{
    /// <summary>
    /// Maps a TournamentParticipant to GetTournamentParticipantDto.
    /// Requires: ApplicationUser and TournamentTopics to be loaded.
    /// </summary>
    public static GetTournamentParticipantDto ToGetDto(this TournamentParticipant participant)
    {
        return new GetTournamentParticipantDto
        {
            Id = participant.Id,
            Username = participant.ApplicationUser?.UserName ?? string.Empty,
            Role = participant.Role,
            ScoreSum = participant.ScoreSum,
            PointsSum = participant.PointsSum,
            TopicsCount = participant.TournamentTopics.Count,
            TournamentId = participant.TournamentId,
            ApplicationUserId = participant.ApplicationUserId
        };
    }

    /// <summary>
    /// Creates a new TournamentParticipant entity from DTO.
    /// Note: TournamentId and ApplicationUserId must be set by the caller before persisting.
    /// </summary>
    public static TournamentParticipant ToNewParticipant(
        this CreateTournamentParticipantDto dto,
        Guid tournamentId,
        Guid applicationUserId)
    {
        return new TournamentParticipant
        {
            Role = dto.Role,
            TournamentId = tournamentId,
            ApplicationUserId = applicationUserId
        };
    }
}
