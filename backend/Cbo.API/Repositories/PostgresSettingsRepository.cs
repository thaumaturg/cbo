using Cbo.API.Data;
using Cbo.API.Models.Domain;

namespace Cbo.API.Repositories;

public class PostgresSettingsRepository : ISettingsRepository
{
    private readonly CboDbContext _dbContext;

    public PostgresSettingsRepository(CboDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Settings> CreateAsync(Settings settings)
    {

        await _dbContext.Settings.AddAsync(settings);
        await _dbContext.SaveChangesAsync();
        return settings;
    }
}
