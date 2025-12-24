namespace Cbo.API.Models.DTO;

public record CreateRoundWithAnswersDto
{
    public required int NumberInMatch { get; set; }
    public required int TopicId { get; set; }
    public List<CreateRoundAnswerDto> Answers { get; set; } = [];
}
