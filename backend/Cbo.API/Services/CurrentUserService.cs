using System.Security.Claims;

namespace Cbo.API.Services;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
    Guid GetRequiredCurrentUserId();
    ClaimsPrincipal GetRequiredCurrentUser();
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private const string NoAuthenticatedUserMessage = "No authenticated user in the current request. This must only be called from [Authorize] endpoints.";

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid? GetCurrentUserId()
    {
        ClaimsPrincipal? user = GetAuthenticatedUser();
        if (user is null)
            return null;

        return Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId)
            ? userId
            : null;
    }

    public Guid GetRequiredCurrentUserId()
    {
        return GetCurrentUserId() ?? throw new InvalidOperationException(NoAuthenticatedUserMessage);
    }

    public ClaimsPrincipal GetRequiredCurrentUser()
    {
        return GetAuthenticatedUser() ?? throw new InvalidOperationException(NoAuthenticatedUserMessage);
    }

    private ClaimsPrincipal? GetAuthenticatedUser()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true ? user : null;
    }
}
