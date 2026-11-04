using System.Security.Claims;
using Cbo.Core.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace Cbo.Api.Auth;

public interface IJwtSecurityStampValidator
{
    Task<bool> IsValidAsync(ClaimsPrincipal principal);
}

/// <summary>
/// Accepts a token only while its user still exists and still has the security stamp the token was issued with.
/// Changing the password or logging out everywhere rotates the stamp, which revokes all of the user's tokens.
/// </summary>
public class JwtSecurityStampValidator(UserManager<ApplicationUser> userManager) : IJwtSecurityStampValidator
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<bool> IsValidAsync(ClaimsPrincipal principal)
    {
        string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string? tokenStamp = principal.FindFirstValue(JwtTokenService.SecurityStampClaimType);
        if (userId is null || tokenStamp is null)
            return false;

        ApplicationUser? user = await _userManager.FindByIdAsync(userId);
        return user is not null && user.SecurityStamp == tokenStamp;
    }
}
