namespace Cbo.Core.Topics;

public record CreateTopicAuthorRequest
{
    public required string Username { get; set; }
}
