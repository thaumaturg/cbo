using Cbo.API.Models.Constants;

namespace Cbo.API.Repositories;

/// <summary>
/// Parameters for updating a tournament participant. This is a repository-layer object, not a DTO.
/// </summary>
public class UpdateTournamentParticipantParameters
{
    public required TournamentParticipantRole Role { get; init; }
}
