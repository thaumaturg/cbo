namespace Cbo.Api.Contracts.Topics;

public record CreateTopicAuthorRequest
{
    public required string Username { get; set; }
}
