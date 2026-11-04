namespace Cbo.Core.Auth;

public record LoginResponse
{
    public required string JwtToken { get; set; }
}
