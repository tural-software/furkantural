using FurkanTural_Application.DTOs.Retention;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Aylık saklama temizliği. Otomatik çalışmaz; yönetici panelden önce önizler, sonra onaylayarak siler. Sınır bugünden 23 ay öncesidir: aydınlatma metinleri 2 yıl saklama ve en geç altı ayda bir imha söz verir, bir aylık pay ayda bir yapılan kontrolün geç kalmasına karşıdır.</summary>
public interface IDataRetentionService
{
    Task<Result<DataRetentionReportDto>> PreviewAsync(CancellationToken cancellationToken = default);

    Task<Result<DataRetentionReportDto>> PurgeAsync(int? userId, CancellationToken cancellationToken = default);
}
