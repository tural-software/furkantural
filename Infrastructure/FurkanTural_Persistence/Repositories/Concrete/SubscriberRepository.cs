using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Domain.Entities;
using FurkanTural_Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FurkanTural_Persistence.Repositories.Concrete;

/// <summary>Taban sınıftaki Admin'li okumalar Dapper ile koşar, bu okuma EF üzerinden: <see cref="Repository{T}"/> ayrımına göre yüklem alan okuma EF'e gider. Süzgeci kaldıran şey IgnoreQueryFilters, yani <see cref="LiveRows"/> koşulu burada elle yazılmaz — süzgecin tanımı tek yerde, BaseEntityConfiguration'da kalır.</summary>
public class SubscriberRepository(FurkanTuralDbContext context) : Repository<Subscriber>(context), ISubscriberRepository
{
    public async Task<Subscriber?> GetByEmailForAdminAsync(string email, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
}
