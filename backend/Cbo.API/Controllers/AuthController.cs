using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost]
    [Route("Register")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesValidationProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto registerUserDto)
    {
        Result result = await _authService.RegisterAsync(registerUserDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok("Account created. You can now log in.");
    }

    [HttpPost]
    [Route("Login")]
    [ProducesResponseType<LoginResponseDto>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserDto loginUserDto)
    {
        Result<LoginResponseDto> result = await _authService.LoginAsync(loginUserDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Route("ChangePassword")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        Result result = await _authService.ChangePasswordAsync(changePasswordDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }
}
