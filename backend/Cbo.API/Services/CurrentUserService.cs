using System.Security.Claims;
using Cbo.API.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace Cbo.API.Services;

public interface ICurrentUserService
{
    Task<ApplicationUser?> GetCurrentUserAsync();
    Task<ApplicationUser> GetRequiredCurrentUserAsync();
}

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    UserManager<ApplicationUser> userManager) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private ApplicationUser? _cachedUser;
    private bool _userResolved;

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        if (_userResolved)
            return _cachedUser;

        _userResolved = true;

        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return null;

        _cachedUser = await _userManager.FindByIdAsync(userId);
        return _cachedUser;
    }

    public async Task<ApplicationUser> GetRequiredCurrentUserAsync()
    {
        ApplicationUser? user = await GetCurrentUserAsync();
        return user ?? throw new UnauthorizedAccessException("Unable to identify the current user.");
    }
}
