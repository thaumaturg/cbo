namespace Cbo.Core.Contracts.Auth;

public record ChangePasswordResponse
{
    public required string JwtToken { get; set; }
}
