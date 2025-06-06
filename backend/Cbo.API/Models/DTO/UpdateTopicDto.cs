namespace Cbo.API.Models.DTO;

public class UpdateTopicDto
{
    public required string Title { get; set; }
    public required int PriorityIndex { get; set; }
    public required bool IsGuest { get; set; }
    public required bool IsPlayed { get; set; }
}
