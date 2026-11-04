namespace Cbo.Core.Accounts;

public record LoginResponse
{
    public required string JwtToken { get; set; }
}
