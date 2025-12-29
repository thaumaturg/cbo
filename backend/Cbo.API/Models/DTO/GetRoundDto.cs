namespace Cbo.API.Models.DTO;

public record GetRoundDto
{
    public required Guid Id { get; set; }
    public required int NumberInMatch { get; set; }
    public required Guid TopicId { get; set; }
    public required string TopicTitle { get; set; }
    public required int TopicPriorityIndex { get; set; }
    public required string TopicOwnerUsername { get; set; }
    public required Guid MatchId { get; set; }
    public required List<GetQuestionDto> Questions { get; set; }
    public required List<GetRoundAnswerDto> RoundAnswers { get; set; }
}
