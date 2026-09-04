using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>Okuma sözleşmesi. Metotların bir bölümü Dapper ile ham SQL, bir bölümü EF Core üzerinden koşar ama sonuç aynı yere varır: adında Admin geçmeyen her okuma yalnızca canlı satırları görür (silinmemiş ve aktif). Yüklem alan aşırı yüklemeler de aynı filtreye tabidir, yani GetAllAsync(x => x.IsDeleted) daima boş döner. Silinmiş veya pasif kayda ulaşmanın tek yolu adında Admin geçen üç metottur; onlar hiçbir filtre uygulamaz.<para>Hiçbir okuma izlenen varlık döndürmez. Dönen nesne üzerinde yapılan değişiklik kendiliğinden kaydedilmez; kalıcı olması için açıkça <see cref="IWriteRepository{T}"/> üzerinden geçmesi gerekir.</para><para>Sayfa numarası 1 tabanlıdır ve descending tarihe değil Id'ye uygulanır. GetAdminSummaryAsync her satır için UpdatedAt, yoksa DeletedAt, o da yoksa CreatedAt değerini alıp en büyüğünü döndürür.</para></summary>
public interface IReadRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<T?> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, bool descending = false, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<EntitySummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllForAdminAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllForAdminPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, bool descending = false, CancellationToken cancellationToken = default);
    Task<int> CountForAdminAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<AdminStatusCountsDto> GetAdminStatusCountsAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
}
