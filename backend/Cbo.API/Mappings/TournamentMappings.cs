using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public static class TournamentMappings
{
    public static GetTournamentDto ToGetDto(this Tournament tournament)
    {
        return new GetTournamentDto
        {
            Id = tournament.Id,
            Title = tournament.Title,
            Description = tournament.Description,
            CurrentStage = tournament.CurrentStage.ToString(),
            CreatedAt = tournament.CreatedAt,
            StartedAt = tournament.StartedAt,
            EndedAt = tournament.EndedAt,
            PlayersPerTournament = tournament.PlayersPerTournament,
            TopicsPerParticipantMax = tournament.TopicsPerParticipantMax,
            TopicsPerParticipantMin = tournament.TopicsPerParticipantMin
        };
    }

    /// <summary>
    /// Maps a CreateTournamentDto to a Tournament domain entity for creation.
    /// </summary>
    public static Tournament ToNewTournament(
        this CreateTournamentDto dto,
        TournamentStage currentStage,
        DateTime createdAt,
        int playersPerTournament,
        int topicsPerParticipantMax,
        int topicsPerParticipantMin)
    {
        return new Tournament
        {
            Title = dto.Title,
            Description = dto.Description,
            CurrentStage = currentStage,
            CreatedAt = createdAt,
            PlayersPerTournament = playersPerTournament,
            TopicsPerParticipantMax = topicsPerParticipantMax,
            TopicsPerParticipantMin = topicsPerParticipantMin
        };
    }
}
