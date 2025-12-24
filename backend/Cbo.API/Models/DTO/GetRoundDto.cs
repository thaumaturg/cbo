namespace Cbo.API.Models.DTO;

public record GetRoundDto
{
    public required int Id { get; set; }
    public required int NumberInMatch { get; set; }
    public required int TopicId { get; set; }
    public required string TopicTitle { get; set; }
    public required int TopicPriorityIndex { get; set; }
    public required string TopicOwnerUsername { get; set; }
    public required int MatchId { get; set; }
    public required List<GetQuestionDto> Questions { get; set; }
    public required List<GetRoundAnswerDto> RoundAnswers { get; set; }
}
