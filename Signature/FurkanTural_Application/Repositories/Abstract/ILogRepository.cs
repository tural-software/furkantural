using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>
/// Kayıt defteri erişimi. Bilerek IRepository&lt;Log&gt; değildir: kayıt eklenir ve okunur, güncelleme veya
/// silme sözleşmeye hiç girmez. Adında Admin geçen metotlar da diğer repo'lardan ayrılır — burada
/// silinmiş ve pasif satırları elerler, oysa <see cref="IReadRepository{T}"/> tarafındaki karşılıkları
/// filtresizdir.
///
/// Süzgeçlerde level birebir eşleşir, project ile message içinde-geçen araması yapar. dateTo günün
/// tamamını kapsar (bir gün eklenip küçüktür ile karşılaştırılır), dolayısıyla saat bilgisi taşıyan bir
/// değer beklenenden bir gün fazlasını getirir. Sıralama Id'ye değil kaydın kendi Date alanına göredir;
/// tek istisna yüklem verilen GetAllPagedAsync'tir, orada hiç sıralama yapılmadığı için sayfaya hangi
/// satırların düşeceği belirsizdir.
/// </summary>
public interface ILogRepository
{
    Task AddAsync(Log log, CancellationToken cancellationToken = default);
    Task<Log?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> GetAllAsync(Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> GetAllPagedAsync(int pageNumber, int pageSize, Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<Log, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> GetAllForAdminPagedAsync(string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountForAdminAsync(string? level, string? project, string? message, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);
    Task<EntitySummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}