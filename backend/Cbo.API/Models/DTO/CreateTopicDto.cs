namespace Cbo.API.Models.DTO;

public class CreateTopicDto
{
    public required string Title { get; set; }
    public required int PriorityIndex { get; set; }
    public required bool IsGuest { get; set; }
    public required bool IsPlayed { get; set; }
    public required ICollection<CreateQuestionDto> Questions { get; set; }
}
