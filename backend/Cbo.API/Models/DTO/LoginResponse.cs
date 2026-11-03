namespace Cbo.API.Models.DTO;

public record LoginResponse
{
    public required string JwtToken { get; set; }
}
