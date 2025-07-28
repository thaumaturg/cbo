using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenRepository _tokenRepository;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenRepository tokenRepository)
    {
        _userManager = userManager;
        _tokenRepository = tokenRepository;
    }

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
            string[] roles = new string[] { "Reader" };

            identityResult = await _userManager.AddToRolesAsync(applicationUser, roles);

            if (identityResult.Succeeded)
            {
                return Ok("Success! You are now able to log in.");
            }
        }

        return BadRequest("Something went wrong");
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
                IList<string>? roles = await _userManager.GetRolesAsync(user);

                if (roles is not null)
                {
                    string jwtToken = _tokenRepository.CreateJWTToken(user, roles.ToList());

                    var response = new LoginResponseDto { JwtToken = jwtToken };

                    return Ok(response);
                }
            }
        }

        return BadRequest("Username or password incorrect");
    }
}
