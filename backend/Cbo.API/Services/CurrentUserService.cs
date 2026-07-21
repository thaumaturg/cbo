using System.Security.Claims;

namespace Cbo.API.Services;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
    Guid GetRequiredCurrentUserId();
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid? GetCurrentUserId()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId)
            ? userId
            : null;
    }

    public Guid GetRequiredCurrentUserId()
    {
        return GetCurrentUserId() ?? throw new UnauthorizedAccessException("Unable to identify the current user.");
    }
}
