namespace Cbo.Core.Contracts.Auth;

public record LoginResponse
{
    public required string JwtToken { get; set; }
}
