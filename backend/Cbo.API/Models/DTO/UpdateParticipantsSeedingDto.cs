namespace Cbo.API.Models.DTO;

public record UpdateParticipantsSeedingDto
{
    /// <summary>
    /// Player participant ids in the desired seeding order. First id becomes seed 1.
    /// Must contain each player of the tournament exactly once.
    /// </summary>
    public required List<Guid> OrderedParticipantIds { get; set; }
}
