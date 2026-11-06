using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cbo.Core.Accounts;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cbo.Tests.Accounts;

public class AccountServiceTests
{
    private const string AlicePassword = "alice-password";

    private readonly InMemoryUserStore _store = new();
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        var errorDescriber = new AccountErrorDescriber();
        var options = new IdentityOptions();
        AccountIdentityOptions.Configure(options);

        _userManager = new UserManager<ApplicationUser>(
            _store,
            Options.Create(options),
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>(errorDescriber)],
            [new PasswordValidator<ApplicationUser>(errorDescriber)],
            new UpperInvariantLookupNormalizer(),
            errorDescriber,
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        _service = new AccountService(_userManager, new FakeJwtTokenService(), new NoCurrentUserService());
    }

    [Fact]
    public async Task An_account_can_log_in_with_its_username()
    {
        await CreateAliceAsync();

        Result<LoginResponse> result = await _service.LoginAsync(
            new LoginRequest { UsernameOrEmail = "alice", Password = AlicePassword });

        Assert.True(result.IsSuccess);
        Assert.Equal("token-for-alice", result.Value.JwtToken);
    }

    [Fact]
    public async Task An_account_can_log_in_with_its_email()
    {
        await CreateAliceAsync();

        Result<LoginResponse> result = await _service.LoginAsync(
            new LoginRequest { UsernameOrEmail = "alice@example.com", Password = AlicePassword });

        Assert.True(result.IsSuccess);
        Assert.Equal("token-for-alice", result.Value.JwtToken);
    }

    [Theory]
    [InlineData("ALICE")]
    [InlineData("Alice")]
    [InlineData("ALICE@EXAMPLE.COM")]
    [InlineData("Alice@Example.com")]
    public async Task The_username_or_email_is_matched_regardless_of_letter_case(string usernameOrEmail)
    {
        await CreateAliceAsync();

        Result<LoginResponse> result = await _service.LoginAsync(
            new LoginRequest { UsernameOrEmail = usernameOrEmail, Password = AlicePassword });

        Assert.True(result.IsSuccess);
        Assert.Equal("token-for-alice", result.Value.JwtToken);
    }

    [Theory]
    [InlineData("alice", "wrong-password")]
    [InlineData("alice@example.com", "wrong-password")]
    [InlineData("bob", AlicePassword)]
    [InlineData("bob@example.com", AlicePassword)]
    public async Task A_wrong_password_and_an_unknown_account_get_the_same_unauthorized_failure(
        string usernameOrEmail, string password)
    {
        await CreateAliceAsync();

        Result<LoginResponse> result = await _service.LoginAsync(
            new LoginRequest { UsernameOrEmail = usernameOrEmail, Password = password });

        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.Unauthorized, result.Failure.Kind);
        Assert.Equal("Incorrect username, email or password.", result.Failure.Message);
        Assert.Empty(result.Failure.FieldErrors);
    }

    [Theory]
    [InlineData("", AlicePassword, "usernameOrEmail", "Enter your username or email.")]
    [InlineData("  ", AlicePassword, "usernameOrEmail", "Enter your username or email.")]
    [InlineData("alice", "", "password", "Enter your password.")]
    [InlineData("alice", " \t", "password", "Enter your password.")]
    public async Task An_empty_login_field_is_invalid(
        string usernameOrEmail, string password, string expectedField, string expectedMessage)
    {
        await CreateAliceAsync();

        Result<LoginResponse> result = await _service.LoginAsync(
            new LoginRequest { UsernameOrEmail = usernameOrEmail, Password = password });

        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.Invalid, result.Failure.Kind);
        FieldError error = Assert.Single(result.Failure.FieldErrors);
        Assert.Equal(expectedField, error.Field);
        Assert.Equal(expectedMessage, error.Message);
    }

    [Fact]
    public async Task Both_empty_login_fields_are_reported_together()
    {
        Result<LoginResponse> result = await _service.LoginAsync(
            new LoginRequest { UsernameOrEmail = "", Password = "" });

        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.Invalid, result.Failure.Kind);
        Assert.Collection(
            result.Failure.FieldErrors,
            error =>
            {
                Assert.Equal("usernameOrEmail", error.Field);
                Assert.Equal("Enter your username or email.", error.Message);
            },
            error =>
            {
                Assert.Equal("password", error.Field);
                Assert.Equal("Enter your password.", error.Message);
            });
    }

    [Fact]
    public async Task Registering_returns_a_token_for_the_new_account()
    {
        Result<RegisterResponse> result = await _service.RegisterAsync(new RegisterRequest
        {
            Email = "carol@example.com",
            Username = "carol",
            Password = "carol-password",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("token-for-carol", result.Value.JwtToken);
        ApplicationUser carol = Assert.Single(_store.Users);
        Assert.Equal("carol@example.com", carol.Email);
        // The real token service needs it to issue a revocable token
        Assert.NotNull(carol.SecurityStamp);
    }

    [Theory]
    [InlineData("car@ol")]
    [InlineData("car-ol")]
    public async Task A_username_with_characters_other_than_latin_letters_digits_and_underscores_is_invalid(
        string username)
    {
        Result<RegisterResponse> result = await _service.RegisterAsync(new RegisterRequest
        {
            Email = "carol@example.com",
            Username = username,
            Password = "carol-password",
        });

        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.Invalid, result.Failure.Kind);
        FieldError error = Assert.Single(result.Failure.FieldErrors);
        Assert.Equal("username", error.Field);
        Assert.Equal("Username can only contain Latin letters, numbers and underscores.", error.Message);
        Assert.Empty(_store.Users);
    }

    [Theory]
    [InlineData("ALICE", "carol@example.com", "username", "This username is already taken.")]
    [InlineData("carol", "Alice@Example.com", "email", "An account with this email already exists.")]
    public async Task A_taken_username_or_email_is_a_conflict_on_that_field(
        string username, string email, string expectedField, string expectedMessage)
    {
        await CreateAliceAsync();

        Result<RegisterResponse> result = await _service.RegisterAsync(new RegisterRequest
        {
            Email = email,
            Username = username,
            Password = "carol-password",
        });

        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.Conflict, result.Failure.Kind);
        FieldError error = Assert.Single(result.Failure.FieldErrors);
        Assert.Equal(expectedField, error.Field);
        Assert.Equal(expectedMessage, error.Message);
    }

    private async Task CreateAliceAsync()
    {
        var alice = new ApplicationUser { UserName = "alice", Email = "alice@example.com" };
        IdentityResult identityResult = await _userManager.CreateAsync(alice, AlicePassword);
        Assert.True(identityResult.Succeeded);
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string CreateToken(ApplicationUser user) => $"token-for-{user.UserName}";
    }

    private sealed class NoCurrentUserService : ICurrentUserService
    {
        public Guid? GetCurrentUserId() => null;

        public Guid GetRequiredCurrentUserId() => throw new NotSupportedException();

        public ClaimsPrincipal GetRequiredCurrentUser() => throw new NotSupportedException();
    }

    private sealed class InMemoryUserStore :
        IUserPasswordStore<ApplicationUser>,
        IUserEmailStore<ApplicationUser>,
        IUserSecurityStampStore<ApplicationUser>
    {
        public List<ApplicationUser> Users { get; } = [];

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user.Id == Guid.Empty)
                user.Id = Guid.NewGuid();
            Users.Add(user);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(Users.Find(user => user.Id.ToString() == userId));

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult(Users.Find(user => user.NormalizedUserName == normalizedUserName));

        public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(Users.Find(user => user.NormalizedEmail == normalizedEmail));

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Email);

        public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.EmailConfirmed);

        public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedEmail);

        public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PasswordHash);

        public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PasswordHash is not null);

        public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.SecurityStamp);

        public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken)
        {
            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
