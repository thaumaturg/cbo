using Cbo.API.Contracts.Auth;
using Cbo.API.Data;
using Cbo.API.Data.Configuration;
using Cbo.API.Models.Domain;
using Cbo.API.Repositories;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Services;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest registerRequest);
    Task<Result<LoginResponse>> LoginAsync(LoginRequest loginRequest);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest);
}

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ITokenRepository tokenRepository,
    ICurrentUserService currentUserService) : IAuthService
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
    private readonly ITokenRepository _tokenRepository = tokenRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result> RegisterAsync(RegisterRequest registerRequest)
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
            return Result.Success();

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
        ApplicationUser? user = await _userManager.FindByEmailAsync(loginRequest.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password))
            return Failure.Unauthorized("The email or password is incorrect.");

        return new LoginResponse { JwtToken = _tokenRepository.CreateJWTToken(user) };
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest)
    {
        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        // A valid token for a user that has been deleted since it was issued
        ApplicationUser? user = await _userManager.FindByIdAsync(currentUserId.ToString());
        if (user is null)
            return Failure.Unauthorized("Your account no longer exists. Please log in again.");

        if (changePasswordRequest.NewPassword == changePasswordRequest.CurrentPassword)
            return InvalidPasswordChange([new FieldError("newPassword", "New password must be different from the current password.")]);

        IdentityResult identityResult = await _userManager.ChangePasswordAsync(
            user,
            changePasswordRequest.CurrentPassword,
            changePasswordRequest.NewPassword);

        if (identityResult.Succeeded)
            return Result.Success();

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

    private static Failure AlreadyRegistered(IEnumerable<FieldError> duplicates) =>
        Failure.Conflict("An account with this username or email already exists.", duplicates);

    private static Failure InvalidPasswordChange(IEnumerable<FieldError> fieldErrors) =>
        Failure.Invalid("Some of the password data is invalid.", fieldErrors);
}
