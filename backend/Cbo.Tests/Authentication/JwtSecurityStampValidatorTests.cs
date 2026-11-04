using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cbo.Api.Authentication;
using Cbo.Core.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cbo.Tests.Authentication;

public class JwtSecurityStampValidatorTests
{
    private static readonly Guid AliceId = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

    private readonly InMemoryUserStore _store = new();
    private readonly JwtSecurityStampValidator _validator;

    public JwtSecurityStampValidatorTests()
    {
        _store.Users.Add(new ApplicationUser { Id = AliceId, UserName = "alice", SecurityStamp = "STAMP-1" });
        _validator = new JwtSecurityStampValidator(CreateUserManager(_store));
    }

    [Fact]
    public async Task A_token_with_the_users_current_stamp_is_valid()
    {
        ClaimsPrincipal principal = TokenPrincipal(AliceId, "STAMP-1");

        Assert.True(await _validator.IsValidAsync(principal));
    }

    [Fact]
    public async Task A_token_is_invalid_once_the_users_stamp_has_changed()
    {
        ClaimsPrincipal principal = TokenPrincipal(AliceId, "STAMP-1");
        _store.Users[0].SecurityStamp = "STAMP-2";

        Assert.False(await _validator.IsValidAsync(principal));
    }

    [Fact]
    public async Task A_token_of_a_deleted_user_is_invalid()
    {
        ClaimsPrincipal principal = TokenPrincipal(AliceId, "STAMP-1");
        _store.Users.Clear();

        Assert.False(await _validator.IsValidAsync(principal));
    }

    [Theory]
    [InlineData("STAMP-1")]
    [InlineData(null)]
    public async Task A_token_issued_without_a_stamp_is_invalid(string? usersStamp)
    {
        _store.Users[0].SecurityStamp = usersStamp;

        Assert.False(await _validator.IsValidAsync(TokenPrincipal(AliceId, stamp: null)));
    }

    private static ClaimsPrincipal TokenPrincipal(Guid userId, string? stamp)
    {
        // The JWT bearer handler maps "sub" to NameIdentifier and leaves "stamp" as is
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, userId.ToString())];
        if (stamp is not null)
            claims.Add(new Claim("stamp", stamp));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static UserManager<ApplicationUser> CreateUserManager(IUserStore<ApplicationUser> store) =>
        new(store, Options.Create(new IdentityOptions()), new PasswordHasher<ApplicationUser>(),
            [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

    private sealed class InMemoryUserStore : IUserStore<ApplicationUser>
    {
        public List<ApplicationUser> Users { get; } = [];

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(Users.Find(user => user.Id.ToString() == userId));

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
