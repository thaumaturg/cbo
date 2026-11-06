using Cbo.Core.Data;
using Cbo.Core.Data.Configuration;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Accounts;

public interface IAccountService
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest registerRequest);
    Task<Result<LoginResponse>> LoginAsync(LoginRequest loginRequest);
    Task<Result<ChangePasswordResponse>> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest);
    Task LogoutEverywhereAsync();
}

public class AccountService(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    ICurrentUserService currentUserService) : IAccountService
{
    private static readonly HashSet<string> PasswordValidationErrorCodes =
    [
        nameof(IdentityErrorDescriber.PasswordTooShort),
        nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars),
        nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
        nameof(IdentityErrorDescriber.PasswordRequiresDigit),
        nameof(IdentityErrorDescriber.PasswordRequiresLower),
        nameof(IdentityErrorDescriber.PasswordRequiresUpper),
    ];

    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest registerRequest)
    {
        var applicationUser = new ApplicationUser
        {
            UserName = registerRequest.Username,
            Email = registerRequest.Email,
            FullName = registerRequest.FullName
        };

        IdentityResult identityResult;
        try
        {
            identityResult = await _userManager.CreateAsync(applicationUser, registerRequest.Password);
        }
        // Lost a race with a simultaneous registration of the same username or email
        catch (DbUpdateException exception) when (PostgresConstraint.IsUniqueViolation(exception, ApplicationUserConfiguration.UserNameIndexName))
        {
            return AlreadyRegistered([new FieldError("username", _userManager.ErrorDescriber.DuplicateUserName(registerRequest.Username).Description)]);
        }
        catch (DbUpdateException exception) when (PostgresConstraint.IsUniqueViolation(exception, ApplicationUserConfiguration.EmailIndexName))
        {
            return AlreadyRegistered([new FieldError("email", _userManager.ErrorDescriber.DuplicateEmail(registerRequest.Email).Description)]);
        }

        if (identityResult.Succeeded)
            return new RegisterResponse { JwtToken = _jwtTokenService.CreateToken(applicationUser) };

        List<FieldError> invalid = [];
        List<FieldError> duplicates = [];
        foreach (IdentityError error in identityResult.Errors)
        {
            switch (error.Code)
            {
                case nameof(IdentityErrorDescriber.DuplicateUserName):
                    duplicates.Add(new FieldError("username", error.Description));
                    break;
                case nameof(IdentityErrorDescriber.DuplicateEmail):
                    duplicates.Add(new FieldError("email", error.Description));
                    break;
                case nameof(IdentityErrorDescriber.InvalidUserName):
                    invalid.Add(new FieldError("username", error.Description));
                    break;
                case nameof(IdentityErrorDescriber.InvalidEmail):
                    invalid.Add(new FieldError("email", error.Description));
                    break;
                case string code when PasswordValidationErrorCodes.Contains(code):
                    invalid.Add(new FieldError("password", error.Description));
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected Identity error '{error.Code}' during registration: {error.Description}");
            }
        }

        if (invalid.Count > 0)
            return Failure.Invalid("Some of the registration data is invalid.", invalid);

        return AlreadyRegistered(duplicates);
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest loginRequest)
    {
        // Until request validation moves to FluentValidation
        List<FieldError> emptyFields = [];
        if (string.IsNullOrWhiteSpace(loginRequest.UsernameOrEmail))
            emptyFields.Add(new FieldError("usernameOrEmail", "Enter your username or email."));
        if (string.IsNullOrWhiteSpace(loginRequest.Password))
            emptyFields.Add(new FieldError("password", "Enter your password."));
        if (emptyFields.Count > 0)
            return Failure.Invalid("Some of the login data is invalid.", emptyFields);

        // Usernames can't contain '@' (AccountIdentityOptions), so an identifier with '@' is always an email
        ApplicationUser? user = loginRequest.UsernameOrEmail.Contains('@')
            ? await _userManager.FindByEmailAsync(loginRequest.UsernameOrEmail)
            : await _userManager.FindByNameAsync(loginRequest.UsernameOrEmail);
        if (user is null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password))
            return Failure.Unauthorized("Incorrect username, email or password.");

        return new LoginResponse { JwtToken = _jwtTokenService.CreateToken(user) };
    }

    public async Task<Result<ChangePasswordResponse>> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest)
    {
        ApplicationUser user = await GetCurrentUserAsync();

        if (changePasswordRequest.NewPassword == changePasswordRequest.CurrentPassword)
            return InvalidPasswordChange([new FieldError("newPassword", "New password must be different from the current password.")]);

        // Also rotates the security stamp, which revokes every token of the user, including the current one
        IdentityResult identityResult = await _userManager.ChangePasswordAsync(
            user,
            changePasswordRequest.CurrentPassword,
            changePasswordRequest.NewPassword);

        if (identityResult.Succeeded)
            return new ChangePasswordResponse { JwtToken = _jwtTokenService.CreateToken(user) };

        // A wrong current password is Invalid, not Unauthorized: a 401 here would end the user's session
        List<FieldError> fieldErrors = identityResult.Errors
            .Select(error => error.Code switch
            {
                nameof(IdentityErrorDescriber.PasswordMismatch) => new FieldError("currentPassword", error.Description),
                string code when PasswordValidationErrorCodes.Contains(code) => new FieldError("newPassword", error.Description),
                _ => throw new InvalidOperationException($"Unexpected Identity error '{error.Code}' while changing the password: {error.Description}"),
            })
            .ToList();

        return InvalidPasswordChange(fieldErrors);
    }

    public async Task LogoutEverywhereAsync()
    {
        ApplicationUser user = await GetCurrentUserAsync();

        IdentityResult identityResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!identityResult.Succeeded)
            throw new InvalidOperationException($"Could not rotate the security stamp of user {user.Id}: {identityResult}");
    }

    // The JWT bearer handler has already rejected tokens of deleted users
    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();
        return await _userManager.FindByIdAsync(currentUserId.ToString())
            ?? throw new InvalidOperationException($"Authenticated user {currentUserId} does not exist.");
    }

    private static Failure AlreadyRegistered(IEnumerable<FieldError> duplicates) =>
        Failure.Conflict("An account with this username or email already exists.", duplicates);

    private static Failure InvalidPasswordChange(IEnumerable<FieldError> fieldErrors) =>
        Failure.Invalid("Some of the password data is invalid.", fieldErrors);
}
