using System.Security.Claims;

namespace Cbo.Core.Auth;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
    Guid GetRequiredCurrentUserId();
    ClaimsPrincipal GetRequiredCurrentUser();
}
