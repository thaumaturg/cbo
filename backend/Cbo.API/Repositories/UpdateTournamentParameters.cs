namespace Cbo.API.Repositories;

/// <summary>
/// Parameters for updating a tournament. This is a repository-layer object, not a DTO.
/// </summary>
public class UpdateTournamentParameters
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required int PlayersPerTournament { get; init; }
    public required int TopicsPerParticipantMax { get; init; }
    public required int TopicsPerParticipantMin { get; init; }
}
