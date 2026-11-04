using Cbo.Core.Models.Domain;

namespace Cbo.Core.Auth;

public interface IJwtTokenService
{
    string CreateToken(ApplicationUser user);
}
