namespace Cbo.Core.Auth;

public record ChangePasswordResponse
{
    public required string JwtToken { get; set; }
}
