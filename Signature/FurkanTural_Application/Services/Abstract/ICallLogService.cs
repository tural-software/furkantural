using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

/// <summary>Arama kayıtları. İlk dört metot SignalR hub akışından çağrılır ve bilinçli olarak <see cref="Wrappers.Result"/> zarfı kullanmaz: hub'ın kullanıcıya döndüreceği bir hata yüzeyi yoktur, kayıt bulunamazsa sessizce geçilir. MarkAnsweredAsync ve MarkEndedAsync sonlanmış bir aramayı yeniden yazmaz, böylece geç gelen kapanış sinyalleri süreyi bozmaz; süre yalnızca arama yanıtlanmışsa hesaplanır. Arama türü ve durumu serbest metin değil <see cref="FurkanTural_Domain.Constants.CallDefinitions"/> değerleridir, geçersiz gelen değer hata yerine sessizce varsayılana çekilir.</summary>
public interface ICallLogService : IBulkService
{
    Task<int> CreateRingingAsync(int callerId, int calleeId, string callType, CancellationToken cancellationToken = default);
    Task<CallParticipantsDto?> GetParticipantsAsync(int callId, CancellationToken cancellationToken = default);
    Task MarkAnsweredAsync(int callId, CancellationToken cancellationToken = default);
    Task MarkEndedAsync(int callId, string status, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CallLogDto>>> GetHistoryAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminCallLogDto>> GetAllPagedForAdminAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<AdminCallLogDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminCallLogDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminCallLogDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminCallLogDto>> GetAllForAdminPagedAsync(AdminListQuery query, string? callType, string? status, CancellationToken cancellationToken = default);
    Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, string? callType, string? status, CancellationToken cancellationToken = default);
}
