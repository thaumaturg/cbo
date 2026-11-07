namespace Cbo.Core.Topics;

/// <summary>A co-author in a create or update topic request. The owner is never one of them.</summary>
public record TopicAuthorRequest
{
    public required string Username { get; set; }
}
