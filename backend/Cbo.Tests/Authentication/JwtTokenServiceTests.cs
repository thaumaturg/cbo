using System;
using Cbo.Api.Authentication;
using Cbo.Core.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Cbo.Tests.Authentication;

public class JwtTokenServiceTests
{
    private const int LifetimeDays = 3;
    private const string AliceStamp = "STAMP-1";

    private static readonly Guid AliceId = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");
    private static readonly TimeSpan ClockTolerance = TimeSpan.FromSeconds(5);

    private static readonly IOptions<JwtOptions> TokenOptions = Options.Create(new JwtOptions
    {
        Key = "test-signing-key-that-is-long-enough-for-hmac-sha256",
        Issuer = "https://cbo.test",
        Audience = "https://cbo.test",
        LifetimeDays = LifetimeDays,
    });

    private static ApplicationUser CreateAlice() => new()
    {
        Id = AliceId,
        UserName = "alice",
        Email = "alice@example.com",
        SecurityStamp = AliceStamp,
    };

    [Fact]
    public void An_issued_token_carries_the_users_id_and_security_stamp()
    {
        string token = new JwtTokenService(TokenOptions).CreateToken(CreateAlice());

        JsonWebToken jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.Equal(AliceId.ToString(), jwt.Subject);
        Assert.Equal(AliceStamp, jwt.GetClaim(JwtTokenService.SecurityStampClaimType).Value);
    }

    [Fact]
    public void An_issued_token_expires_after_the_configured_lifetime()
    {
        string token = new JwtTokenService(TokenOptions).CreateToken(CreateAlice());

        JsonWebToken jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        TimeSpan expected = TimeSpan.FromDays(LifetimeDays);
        Assert.InRange(jwt.ValidTo - jwt.IssuedAt, expected - ClockTolerance, expected + ClockTolerance);
    }
}
