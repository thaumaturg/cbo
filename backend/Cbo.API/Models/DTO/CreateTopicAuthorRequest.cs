namespace Cbo.API.Models.DTO;

public record CreateTopicAuthorRequest
{
    public required string Username { get; set; }
}
