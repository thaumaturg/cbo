using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ITokenRepository
{
    string CreateJWTToken(ApplicationUser user);
}
