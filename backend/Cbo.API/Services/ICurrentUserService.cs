using Cbo.API.Models.Domain;

namespace Cbo.API.Services;

public interface ICurrentUserService
{
    Task<ApplicationUser?> GetCurrentUserAsync();
    Task<ApplicationUser> GetRequiredCurrentUserAsync();
}
