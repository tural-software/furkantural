using FurkanTural_Application.Services.Abstract;
using FurkanTural_Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FurkanTural_Persistence.Interceptors;

/// <summary>Zaman damgalarını kaydetme anında basar; eşzamanlı ve asenkron kaydetme yollarının ikisini de karşılar, yalnızca BaseEntity türevlerine dokunur.<para>Saat bir kez okunup bütün girdilere aynı değer yazılır, dolayısıyla tek kaydetmede eklenen satırlar birebir aynı CreatedAt'i taşır — zaman damgasını ayırt edici sayan okumalar bunu hesaba katmalıdır. DeletedAt yalnızca boşken doldurulur; zaten silinmiş bir kayıt yeniden kaydedilirse ilk silinme anı korunur.</para><para>Eklemede IsActive ve IsDeleted, çağıranın verdiği değere bakılmaksızın sabitlenir; bu yolla pasif veya silinmiş bir kayıt doğrudan oluşturulamaz.</para></summary>
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
                    if (entry.Entity.IsDeleted && entry.Entity.DeletedAt is null)
                        entry.Entity.DeletedAt = now;
                    break;
            }
        }
    }
}
