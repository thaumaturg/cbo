namespace Cbo.API.Models.DTO;

public class GetRoundDto
{
    public required int Id { get; set; }
    public required int NumberInMatch { get; set; }
    public required int TopicId { get; set; }
    public required int MatchId { get; set; }
}
