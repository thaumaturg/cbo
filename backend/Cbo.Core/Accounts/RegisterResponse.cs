namespace Cbo.Core.Accounts;

public record RegisterResponse
{
    public required string JwtToken { get; set; }
}
