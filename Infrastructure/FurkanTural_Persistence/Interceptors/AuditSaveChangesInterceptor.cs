using FurkanTural_Application.Services.Abstract;
using FurkanTural_Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FurkanTural_Persistence.Interceptors;

/// <summary>
/// Audit zaman damgalarını tek kanonik saat kaynağından (<see cref="IClock"/>) set eder:
/// Added → CreatedAt, Modified → UpdatedAt, soft-delete (IsDeleted=true) → DeletedAt.
/// </summary>
public sealed class AuditSaveChangesInterceptor(IClock clock) : SaveChangesInterceptor
{
    private readonly IClock _clock = clock;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;

        var now = _clock.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.IsActive = true;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    // Soft-delete: DeletedAt yalnızca ilk silmede damgalanır.
                    if (entry.Entity.IsDeleted && entry.Entity.DeletedAt is null)
                        entry.Entity.DeletedAt = now;
                    break;
            }
        }
    }
}
