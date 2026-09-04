using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Subscriber;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Bülten aboneleri. UnsubscribeAsync kaydı yumuşak siler; canlı okumalar silinmiş satırı görmediği için aynı adres yeniden abone olduğunda eski kayıt canlandırılmaz, yeni bir satır açılır. Kayıt hâlâ canlıyken ikinci kez abone olma denemesi ise hata döner.</summary>
public interface ISubscriberService : IService<SubscriberDto, CreateSubscriberDto, UpdateSubscriberDto>
{
    Task<Result> SubscribeAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> UnsubscribeAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<AdminSubscriberDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminSubscriberDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminSubscriberDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminSubscriberDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminSubscriberDto>> GetAllForAdminPagedAsync(AdminListQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, CancellationToken cancellationToken = default);
}
