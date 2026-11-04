using System.Security.Claims;

namespace Cbo.Core.Services;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
    Guid GetRequiredCurrentUserId();
    ClaimsPrincipal GetRequiredCurrentUser();
}
