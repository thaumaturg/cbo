using System;
using System.Collections.Generic;
using Cbo.Api.Models.Domain;
using Cbo.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Cbo.Tests.Auth;

public class JwtTokenServiceTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-signing-key-that-is-long-enough-for-hmac-sha256",
            ["Jwt:Issuer"] = "https://cbo.test",
            ["Jwt:Audience"] = "https://cbo.test",
        })
        .Build();

    [Fact]
    public void An_issued_token_carries_the_users_id_and_security_stamp()
    {
        var user = new ApplicationUser
        {
            Id = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
            UserName = "alice",
            Email = "alice@example.com",
            SecurityStamp = "STAMP-1",
        };

        string token = new JwtTokenService(Configuration).CreateToken(user);

        JsonWebToken jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.Equal("6f9619ff-8b86-d011-b42d-00c04fc964ff", jwt.Subject);
        Assert.Equal("STAMP-1", jwt.GetClaim("stamp").Value);
    }
}
