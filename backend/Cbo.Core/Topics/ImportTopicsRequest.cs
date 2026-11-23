namespace Cbo.Core.Topics;

public record ImportTopicsRequest
{
    public required bool IsAuthor { get; set; }
    public required ICollection<ImportTopicRequest> Topics { get; set; }
}
