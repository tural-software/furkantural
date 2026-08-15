using FurkanTural_Domain.Entities.Common;

namespace FurkanTural_Application.Repositories.Abstract;

/// <summary>
/// Yazma sözleşmesi. Buradaki hiçbir metot veri tabanına gitmez, yalnızca değişikliği kuyruğa alır;
/// kalıcı olması için <see cref="IUnitOfWork.SaveChangesAsync"/> gerekir. Adlandırma yanıltıcıdır:
/// DeleteAsync satırı tablodan gerçekten siler, yumuşak silme ayrı metottadır — servis katmanındaki
/// aynı adlı metot ise yumuşak sildiği için DeleteAsync iki katmanda zıt anlamlıdır.
///
/// SoftDeleteAsync kaydı hem silinmiş hem pasif işaretler; RestoreAsync bunun tersini yapıp DeletedAt'i
/// de temizler, dolayısıyla kayıt silinmeden önce pasif olsa bile aktif olarak geri döner. Zaman
/// damgaları elle set edilmez, kaydetme anında damgalanır — aynı yerde yeni eklenen kaydın
/// IsActive/IsDeleted alanları da sabitlenir, bu yüzden AddAsync ile pasif kayıt oluşturulamaz.
/// UpdateAsync varlığın tamamını değişmiş sayar; tek alan değişse bile bütün sütunlar yazılır.
/// </summary>
public interface IWriteRepository<T> where T : BaseEntity
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task RestoreAsync(T entity, CancellationToken cancellationToken = default);
}