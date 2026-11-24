using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cbo.Core.Accounts;
using Cbo.Core.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cbo.Api.Authentication;

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public const string SecurityStampClaimType = "stamp";

    private readonly JwtOptions _options = options.Value;

    public string CreateToken(ApplicationUser user)
    {
        string email = user.Email ?? throw new InvalidOperationException($"User {user.Id} has no email.");
        string userName = user.UserName ?? throw new InvalidOperationException($"User {user.Id} has no username.");
        string securityStamp = user.SecurityStamp ?? throw new InvalidOperationException($"User {user.Id} has no security stamp.");

        long issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var claims = new List<Claim>
        {
            // Standard JWT claims
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, issuedAt.ToString(), ClaimValueTypes.Integer64),

            // OIDC standard claims
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, user.FullName ?? string.Empty),
            new("preferred_username", userName),
            new("email_verified", user.EmailConfirmed.ToString().ToLower()),

            // Checked on every request, so rotating the stamp revokes the user's tokens
            new(SecurityStampClaimType, securityStamp)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: DateTime.UtcNow.AddDays(_options.LifetimeDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
