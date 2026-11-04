using Cbo.Core.Domain;

namespace Cbo.Core.Accounts;

public interface IJwtTokenService
{
    string CreateToken(ApplicationUser user);
}
