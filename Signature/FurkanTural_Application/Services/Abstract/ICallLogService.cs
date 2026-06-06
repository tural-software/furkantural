using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Wrappers;

namespace FurkanTural_Application.Services.Abstract;

public interface ICallLogService
{
    // ── Hub yaşam döngüsü ──
    /// <summary>Yeni "Ringing" arama kaydı oluşturur; callId döner.</summary>
    Task<int> CreateRingingAsync(int callerId, int calleeId, string callType, CancellationToken cancellationToken = default);
    Task<CallParticipantsDto?> GetParticipantsAsync(int callId, CancellationToken cancellationToken = default);
    Task MarkAnsweredAsync(int callId, CancellationToken cancellationToken = default);
    /// <summary>Aramayı sonlandırır. <paramref name="status"/>: Ended/Rejected/Missed/Canceled/Failed.</summary>
    Task MarkEndedAsync(int callId, string status, CancellationToken cancellationToken = default);

    // ── Üye ──
    Task<Result<IEnumerable<CallLogDto>>> GetHistoryAsync(int currentUserId, CancellationToken cancellationToken = default);

    // ── Admin ──
    Task<PagedResult<AdminCallLogDto>> GetAllPagedForAdminAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<AdminCallLogDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AdminCallLogDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<AdminCallLogDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default);
    Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
}
