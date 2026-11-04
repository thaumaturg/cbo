using System.Security.Claims;

namespace Cbo.Core.Accounts;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
    Guid GetRequiredCurrentUserId();
    ClaimsPrincipal GetRequiredCurrentUser();
}
