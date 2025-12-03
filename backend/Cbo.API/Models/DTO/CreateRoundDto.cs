namespace Cbo.API.Models.DTO;

public record CreateRoundDto
{
    public required int NumberInMatch { get; set; }
    public required int TopicId { get; set; }
    public required int MatchId { get; set; }
}
