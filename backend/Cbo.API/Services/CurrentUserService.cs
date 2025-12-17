using System.Security.Claims;
using Cbo.API.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace Cbo.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private ApplicationUser? _cachedUser;
    private bool _userResolved;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        if (_userResolved)
            return _cachedUser;

        _userResolved = true;

        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        string? username = user.Identity.Name;
        if (string.IsNullOrEmpty(username))
            return null;

        _cachedUser = await _userManager.FindByNameAsync(username);
        return _cachedUser;
    }

    public async Task<ApplicationUser> GetRequiredCurrentUserAsync()
    {
        ApplicationUser? user = await GetCurrentUserAsync();
        return user ?? throw new UnauthorizedAccessException("Unable to identify the current user.");
    }
}
