using Cbo.Core.Models.Domain;

namespace Cbo.Core.Services;

public interface IJwtTokenService
{
    string CreateToken(ApplicationUser user);
}
