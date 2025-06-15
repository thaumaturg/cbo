namespace Cbo.API.Models.DTO;

public class UpdateRoundDto
{
    public required int NumberInMatch { get; set; }
    public required int TopicId { get; set; }
    public required int MatchId { get; set; }
}
