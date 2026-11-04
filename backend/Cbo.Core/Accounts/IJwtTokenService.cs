using Cbo.Core.Models.Domain;

namespace Cbo.Core.Accounts;

public interface IJwtTokenService
{
    string CreateToken(ApplicationUser user);
}
