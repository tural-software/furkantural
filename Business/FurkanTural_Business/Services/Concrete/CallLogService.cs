using FurkanTural_Application.DTOs.Call;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

public class CallLogService(IUnitOfWork unitOfWork, IClock clock) : ICallLogService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IClock _clock = clock;

    private static readonly HashSet<string> _terminal = new(StringComparer.OrdinalIgnoreCase)
    {
        CallDefinitions.Statuses.Ended, CallDefinitions.Statuses.Rejected,
        CallDefinitions.Statuses.Missed, CallDefinitions.Statuses.Canceled,
        CallDefinitions.Statuses.Failed
    };

    public async Task<int> CreateRingingAsync(int callerId, int calleeId, string callType, CancellationToken cancellationToken = default)
    {
        var entity = new CallLog
        {
            CallerId = callerId,
            CalleeId = calleeId,
            CallType = CallDefinitions.IsValidType(callType) ? callType : CallDefinitions.Types.Audio,
            Status = CallDefinitions.Statuses.Ringing,
            StartedAt = _clock.UtcNow
        };
        await _unitOfWork.CallLogs.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<CallParticipantsDto?> GetParticipantsAsync(int callId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CallLogs.GetByIdAsync(callId, cancellationToken);
        return entity?.ToParticipantsDto();
    }

    public async Task MarkAnsweredAsync(int callId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CallLogs.GetByIdAsync(callId, cancellationToken);
        if (entity is null || _terminal.Contains(entity.Status ?? "")) return;

        entity.Status = CallDefinitions.Statuses.Answered;
        entity.AnsweredAt = _clock.UtcNow;
        await _unitOfWork.CallLogs.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkEndedAsync(int callId, string status, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CallLogs.GetByIdAsync(callId, cancellationToken);
        if (entity is null || _terminal.Contains(entity.Status ?? "")) return;

        var now = _clock.UtcNow;
        entity.Status = _terminal.Contains(status) ? status : CallDefinitions.Statuses.Ended;
        entity.EndedAt = now;
        if (entity.AnsweredAt is { } answered)
            entity.DurationSeconds = Math.Max(0, (int)(now - answered).TotalSeconds);

        await _unitOfWork.CallLogs.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<IEnumerable<CallLogDto>>> GetHistoryAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var entities = (await _unitOfWork.CallLogs.GetAllAsync(
            x => x.CallerId == currentUserId || x.CalleeId == currentUserId, cancellationToken))
            .OrderByDescending(e => e.StartedAt)
            .Take(100)
            .ToList();

        var userCache = new Dictionary<int, User?>();
        async Task<User?> GetUser(int id)
        {
            if (!userCache.TryGetValue(id, out var u))
                userCache[id] = u = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            return u;
        }

        var list = new List<CallLogDto>();
        foreach (var e in entities)
        {
            var outgoing = e.CallerId == currentUserId;
            var otherId = outgoing ? e.CalleeId : e.CallerId;
            var other = await GetUser(otherId);
            list.Add(new CallLogDto
            {
                Id = e.Id,
                OtherUserId = otherId,
                OtherUsername = other?.Username,
                OtherDisplayName = other?.DisplayName,
                OtherAvatarUrl = other?.AvatarUrl,
                Direction = outgoing ? "Outgoing" : "Incoming",
                CallType = e.CallType,
                Status = e.Status,
                StartedAt = e.StartedAt,
                DurationSeconds = e.DurationSeconds
            });
        }

        return Result<IEnumerable<CallLogDto>>.Ok(list);
    }

    public async Task<PagedResult<AdminCallLogDto>> GetAllPagedForAdminAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = (await _unitOfWork.CallLogs.GetAllForAdminAsync(cancellationToken))
            .OrderByDescending(e => e.StartedAt)
            .ToList();

        var userIds = all.SelectMany(e => new[] { e.CallerId, e.CalleeId }).Distinct().ToList();
        var users = new Dictionary<int, User?>();
        foreach (var id in userIds)
            users[id] = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);

        var page = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(e =>
        {
            var dto = e.ToAdminDto();
            dto.CallerName = NameOf(users, e.CallerId);
            dto.CalleeName = NameOf(users, e.CalleeId);
            return dto;
        });

        return PagedResult<AdminCallLogDto>.Ok(page, all.Count, pageNumber, pageSize);
    }

    public async Task<Result<AdminCallLogDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CallLogs.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCallLogDto>.Fail("Arama kaydı bulunamadı.", statusCode: 404);

        return Result<AdminCallLogDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<AdminCallLogDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CallLogs.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCallLogDto>.Fail("Arama kaydı bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminCallLogDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.CallLogs.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminCallLogDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<AdminCallLogDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CallLogs.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminCallLogDto>.Fail("Arama kaydı bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminCallLogDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.CallLogs.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminCallLogDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.CallLogs.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private async Task<AdminCallLogDto> EnrichAsync(CallLog entity, CancellationToken cancellationToken)
    {
        var dto = entity.ToAdminDto();
        var caller = await _unitOfWork.Users.GetByIdAsync(entity.CallerId, cancellationToken);
        var callee = await _unitOfWork.Users.GetByIdAsync(entity.CalleeId, cancellationToken);
        dto.CallerName = caller?.DisplayName ?? caller?.Username;
        dto.CalleeName = callee?.DisplayName ?? callee?.Username;
        return dto;
    }

    private static string? NameOf(Dictionary<int, User?> users, int id)
        => users.TryGetValue(id, out var u) ? (u?.DisplayName ?? u?.Username) : null;
}