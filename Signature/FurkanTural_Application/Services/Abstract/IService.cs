using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Varlık servislerinin ortak CRUD sözleşmesi. Silme daima yumuşaktır: DeleteAsync kaydı IsDeleted yapıp pasife çeker, satır tabloda kalır. Buradaki okumalar yalnızca canlı satırları görür (silinmemiş ve aktif), bu yüzden pasife alınmış bir kayıt GetByIdAsync ile bulunamaz ve UpdateAsync/DeleteAsync 404 döner; önce ToggleActiveAsync ile geri açılması gerekir.<para>Türeyen arayüzler yönetim paneli için aynı beşliyi tekrarlar ve hepsinde davranış birebir aynıdır: GetAllForAdminAsync/GetByIdForAdminAsync filtresiz okur (silinmiş ve pasif kayıtlar dahil), ToggleActiveAsync aktifliği ters çevirir ama silinmiş kayıtta 400 döner, RestoreAsync yalnızca silinmiş kayıtta çalışır ve kaydı her hâlükârda aktif hâle getirir — silinmeden önce pasif olsa bile — GetAdminSummaryAsync ise adet ile son değişiklik tarihini döndürür.</para></summary>
public interface IService<TDto, TCreateDto, TUpdateDto>
{
    Task<Result<TDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<TDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<TDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<TDto>> CreateAsync(TCreateDto dto, CancellationToken cancellationToken = default);
    Task<Result<TDto>> UpdateAsync(TUpdateDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, int? deletedBy, CancellationToken cancellationToken = default);
}
