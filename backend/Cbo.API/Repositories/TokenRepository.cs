using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cbo.API.Models.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Cbo.API.Repositories;

public interface ITokenRepository
{
    string CreateJWTToken(ApplicationUser user);
}

public class TokenRepository(IConfiguration configuration) : ITokenRepository
{
    private readonly IConfiguration _configuration = configuration;

    public string CreateJWTToken(ApplicationUser user)
    {
        string email = user.Email ?? throw new InvalidOperationException($"User {user.Id} has no email.");
        string userName = user.UserName ?? throw new InvalidOperationException($"User {user.Id} has no username.");

        string jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Configuration value 'Jwt:Key' is missing.");

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
            new("email_verified", user.EmailConfirmed.ToString().ToLower())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
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
