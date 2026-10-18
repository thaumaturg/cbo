using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cbo.API.Data;

public static class DatabaseFacadeExtensions
{
    /// <summary>
    /// Runs <paramref name="action"/> in a transaction: commits when it completes, rolls back and rethrows when it throws.
    /// </summary>
    public static async Task ExecuteInTransactionAsync(this DatabaseFacade database, Func<Task> action)
    {
        await using IDbContextTransaction transaction = await database.BeginTransactionAsync();
        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
