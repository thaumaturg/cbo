namespace Cbo.Core.Contracts.TournamentTopics;

public record ChosenTopicRequest
{
    public required Guid TopicId { get; set; }
    public required int PriorityIndex { get; set; }
}
