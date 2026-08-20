using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cbo.API.Data.Interceptors;

/// <summary>
/// Writes audit timestamps on save for every <see cref="IAuditable"/> entity:
/// <see cref="IAuditable.CreatedAt"/> on insert, <see cref="IAuditable.UpdatedAt"/> on update.
/// </summary>
public class AuditSaveChangesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTimestamps(DbContext? context)
    {
        if (context is null)
            return;

        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (EntityEntry<IAuditable> entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;

                // CreatedAt is immutable after insert, even if the caller changed it.
                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
            }
        }
    }
}
