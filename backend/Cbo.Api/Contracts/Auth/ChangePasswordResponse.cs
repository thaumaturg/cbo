namespace Cbo.Api.Contracts.Auth;

public record ChangePasswordResponse
{
    public required string JwtToken { get; set; }
}
