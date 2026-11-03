using System.Security.Claims;
using Cbo.API.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace Cbo.API.Services;

public interface IJwtSecurityStampValidator
{
    Task<bool> IsValidAsync(ClaimsPrincipal principal);
}

/// <summary>
/// Accepts a token only while its user still exists and still has the security stamp the token was issued with.
/// Identity rotates the stamp on a password change, so that revokes all of the user's tokens.
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
