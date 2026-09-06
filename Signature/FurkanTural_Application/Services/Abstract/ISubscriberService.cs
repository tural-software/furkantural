using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Subscriber;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Bülten abonelerinin yönetim tarafı: listeleme, süzme, aktiflik ve toplu işlem. Ziyaretçinin gördüğü abonelik akışı burada değil <see cref="INewsletterService"/>'tedir — o akışın tek sorusu adresin sahipliğidir ve buradaki hiçbir uç o soruyu sormaz.<para>Yumuşak silme kalıbı burada da geçerlidir: listeden düşen abone satırı durmaya devam eder, dolayısıyla aynı adres yeniden abone olduğunda yeni satır açılmaz, duran satır geri açılır.</para></summary>
public interface ISubscriberService : IService<SubscriberDto, CreateSubscriberDto, UpdateSubscriberDto>, IBulkService
{
    Task<Result<IEnumerable<AdminSubscriberDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminSubscriberDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminSubscriberDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminSubscriberDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminSubscriberDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default);
}
