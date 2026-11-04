namespace Cbo.Core.Accounts;

public record ChangePasswordResponse
{
    public required string JwtToken { get; set; }
}
