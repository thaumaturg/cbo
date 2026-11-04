using Cbo.Core.Contracts.Topics;

namespace Cbo.Core.Contracts.Rounds;

public record RoundResponse
{
    public required Guid Id { get; set; }
    public required int NumberInMatch { get; set; }
    public required bool IsOverrideMode { get; set; }
    public required Guid TopicId { get; set; }
    public required string TopicTitle { get; set; }
    public required int TopicPriorityIndex { get; set; }
    public required string TopicOwnerUsername { get; set; }
    public required Guid MatchId { get; set; }
    public required List<QuestionResponse> Questions { get; set; }
    public required List<RoundAnswerResponse> RoundAnswers { get; set; }
}
