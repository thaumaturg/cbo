using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cbo.API.Models.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Cbo.API.Repositories;

public class TokenRepository : ITokenRepository
{
    private readonly IConfiguration _configuration;

    public TokenRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateJWTToken(ApplicationUser user)
    {
        long issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var claims = new List<Claim>
        {
            // Standard JWT claims
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, issuedAt.ToString(), ClaimValueTypes.Integer64),

            // OIDC standard claims
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName ?? string.Empty),
            new("preferred_username", user.UserName),
            new("email_verified", user.EmailConfirmed.ToString().ToLower())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddDays(14),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
