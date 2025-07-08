using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;

    public AuthController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost]
    [Route("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto createUserDto)
    {
        var identityUser = new IdentityUser
        {
            UserName = createUserDto.Username,
            Email = createUserDto.Email,
        };

        IdentityResult identityResult = await _userManager.CreateAsync(identityUser, createUserDto.Password);

        if (identityResult.Succeeded)
        {
            // Add roles to this User
            if (createUserDto.Roles is not null && createUserDto.Roles.Length != 0)
            {
                identityResult = await _userManager.AddToRolesAsync(identityUser, createUserDto.Roles);

                if (identityResult.Succeeded)
                {
                    return Ok("Success! You are now able to log in.");
                }
            }
        }

        return BadRequest("Something went wrong");
    }

    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto loginUserDto)
    {
        IdentityUser? user = await _userManager.FindByEmailAsync(loginUserDto.Email);

        if (user != null)
        {
            bool checkPasswordResult = await _userManager.CheckPasswordAsync(user, loginUserDto.Password);

            if (checkPasswordResult)
            {
                // TODO: Create Token

                return Ok();
            }
        }

        return BadRequest("Username or password incorrect");
    }
}
