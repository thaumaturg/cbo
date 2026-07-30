using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    ITokenRepository tokenRepository,
    ICurrentUserService currentUserService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ITokenRepository _tokenRepository = tokenRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpPost]
    [Route("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto createUserDto)
    {
        var applicationUser = new ApplicationUser
        {
            UserName = createUserDto.Username,
            Email = createUserDto.Email,
            FullName = createUserDto.FullName
        };

        IdentityResult identityResult = await _userManager.CreateAsync(applicationUser, createUserDto.Password);

        if (identityResult.Succeeded)
        {
            return Ok("Account created. You can now log in.");
        }

        return BadRequest("Registration failed. Please check that your data.");
    }

    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto loginUserDto)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(loginUserDto.Email);

        if (user != null)
        {
            bool checkPasswordResult = await _userManager.CheckPasswordAsync(user, loginUserDto.Password);

            if (checkPasswordResult)
            {
                string jwtToken = _tokenRepository.CreateJWTToken(user);

                var response = new LoginResponseDto { JwtToken = jwtToken };

                return Ok(response);
            }
        }

        return BadRequest("Login failed. Please check your credentials.");
    }

    [HttpPost]
    [Route("ChangePassword")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (changePasswordDto.NewPassword == changePasswordDto.CurrentPassword)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(ChangePasswordDto.NewPassword)] = ["New password must be different from the current password."]
            }));
        }

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        ApplicationUser? user = await _userManager.FindByIdAsync(currentUserId.ToString());

        if (user is null)
            return Unauthorized();

        IdentityResult identityResult = await _userManager.ChangePasswordAsync(
            user,
            changePasswordDto.CurrentPassword,
            changePasswordDto.NewPassword);

        if (identityResult.Succeeded)
            return NoContent();

        Dictionary<string, string[]> errors = identityResult.Errors
            .GroupBy(error => error.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
                ? nameof(ChangePasswordDto.CurrentPassword)
                : nameof(ChangePasswordDto.NewPassword))
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());

        return ValidationProblem(new ValidationProblemDetails(errors));
    }
}
