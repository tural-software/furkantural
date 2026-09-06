using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Yönetim listesinde seçili satırlara tek istekte uygulanan işlemler. Tekil uçlarla aynı kuralları izler: silinmiş kayıt yeniden silinmez, silinmemiş kayıt geri yüklenmez, silinmiş kaydın aktifliği değiştirilmez. Uygun durumda olmayan ya da bulunamayan kimlikler hata değildir; işlenmez ve yanıtta listelenir.<para>Aktiflik için "tersine çevir" yoktur: karışık bir seçimde yön belirsiz kalırdı, bu yüzden Activate ve Deactivate ayrı ayrı istenir.</para></summary>
public interface IBulkService
{
    Task<Result<BulkActionResultDto>> BulkAsync(BulkAction action, IReadOnlyCollection<int> ids, int? userId, CancellationToken cancellationToken = default);
}
