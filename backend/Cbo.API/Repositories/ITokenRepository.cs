using Microsoft.AspNetCore.Identity;

namespace Cbo.API.Repositories;

public interface ITokenRepository
{
    string CreateJWTToken(IdentityUser user, List<string> roles);
}
