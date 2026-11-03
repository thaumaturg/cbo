namespace Cbo.API.Contracts.Auth;

public record LoginResponse
{
    public required string JwtToken { get; set; }
}
