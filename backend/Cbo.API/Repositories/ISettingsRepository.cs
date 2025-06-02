using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public interface ISettingsRepository
{
    Task<Settings> CreateAsync(Settings settings);
}
