namespace Cbo.API.Models.DTO;

public class GetTopicDto
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public required bool IsGuest { get; set; }
    public required bool IsPlayed { get; set; }
    public ICollection<GetQuestionDto> Questions { get; set; } = [];
}
