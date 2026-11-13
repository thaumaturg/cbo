namespace Cbo.Core.TournamentTopics;

public record TournamentTopicRequest
{
    public required Guid TopicId { get; set; }
    public required int PriorityIndex { get; set; }
}
