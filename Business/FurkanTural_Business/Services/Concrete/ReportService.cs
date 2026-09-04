using System.Linq.Expressions;
using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.DTOs.Report;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Application.Wrappers;
using FurkanTural_Business.Helpers;
using FurkanTural_Business.Mappers;
using FurkanTural_Domain.Constants;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Services.Concrete;

public class ReportService(IUnitOfWork unitOfWork, ActivityLogger activityLogger) : IReportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivityLogger _activityLogger = activityLogger;

    public async Task<Result> CreateAsync(int reporterId, CreateReportDto dto, CancellationToken cancellationToken = default)
    {
        if (!ReportDefinitions.IsValidTargetType(dto.TargetType))
            return Result.Fail("Geçersiz şikayet türü.");

        if (dto.ReportedUserId is { } reportedId)
        {
            if (reportedId == reporterId)
                return Result.Fail("Kendinizi şikayet edemezsiniz.");

            var target = await _unitOfWork.Users.GetByIdAsync(reportedId, cancellationToken);
            if (target is null)
                return Result.Fail("Şikayet edilen kullanıcı bulunamadı.", statusCode: 404);
        }

        var entity = new Report
        {
            ReporterId = reporterId,
            ReportedUserId = dto.ReportedUserId,
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            Reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim(),
            Status = ReportDefinitions.Statuses.Pending
        };
        await _unitOfWork.Reports.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync($"Şikayet oluşturuldu. Reporter: {reporterId}, Tür: {dto.TargetType}", cancellationToken);

        return Result.Ok("Şikayetiniz alındı. En kısa sürede incelenecektir.");
    }

    public async Task<Result<IEnumerable<AdminReportDto>>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var entities = (await _unitOfWork.Reports.GetAllForAdminAsync(cancellationToken))
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        var userIds = entities.SelectMany(e => new[] { e.ReporterId, e.ReportedUserId ?? 0 })
            .Where(id => id > 0).Distinct().ToList();
        var users = new Dictionary<int, User?>();
        foreach (var id in userIds)
            users[id] = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);

        var dtos = entities.Select(e =>
        {
            var dto = e.ToAdminDto();
            dto.ReporterName = NameOf(users, e.ReporterId);
            dto.ReportedUserName = e.ReportedUserId is { } rid ? NameOf(users, rid) : null;
            return dto;
        });

        return Result<IEnumerable<AdminReportDto>>.Ok(dtos);
    }

    public async Task<Result<AdminReportDto>> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Reports.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminReportDto>.Fail("Şikayet bulunamadı.", statusCode: 404);

        return Result<AdminReportDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<AdminReportDto>> UpdateStatusAsync(int id, string status, string? adminNote, int? updatedBy, CancellationToken cancellationToken = default)
    {
        if (!ReportDefinitions.IsValidStatus(status))
            return Result<AdminReportDto>.Fail("Geçersiz durum.");

        var entity = await _unitOfWork.Reports.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminReportDto>.Fail("Şikayet bulunamadı.", statusCode: 404);

        entity.Status = status;
        entity.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? entity.AdminNote : adminNote.Trim();
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Reports.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminReportDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<AdminReportDto>> ToggleActiveAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Reports.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminReportDto>.Fail("Şikayet bulunamadı.", statusCode: 404);

        if (entity.IsDeleted)
            return Result<AdminReportDto>.Fail("Silinmiş kayıtların aktifliği değiştirilemez.", statusCode: 400);

        entity.IsActive = !entity.IsActive;
        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Reports.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminReportDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<AdminReportDto>> RestoreAsync(int id, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Reports.GetByIdForAdminAsync(id, cancellationToken);
        if (entity is null)
            return Result<AdminReportDto>.Fail("Şikayet bulunamadı.", statusCode: 404);

        if (!entity.IsDeleted)
            return Result<AdminReportDto>.Fail("Bu kayıt silinmemiş, geri yükleme yapılamaz.", statusCode: 400);

        entity.UpdatedBy = updatedBy;
        await _unitOfWork.Reports.RestoreAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminReportDto>.Ok(await EnrichAsync(entity, cancellationToken));
    }

    public async Task<Result<EntitySummaryDto>> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _unitOfWork.Reports.GetAdminSummaryAsync(cancellationToken);
        return Result<EntitySummaryDto>.Ok(summary);
    }

    private async Task<AdminReportDto> EnrichAsync(Report entity, CancellationToken cancellationToken)
    {
        var dto = entity.ToAdminDto();
        var reporter = await _unitOfWork.Users.GetByIdAsync(entity.ReporterId, cancellationToken);
        dto.ReporterName = reporter?.DisplayName ?? reporter?.Username;
        if (entity.ReportedUserId is { } rid)
        {
            var reported = await _unitOfWork.Users.GetByIdAsync(rid, cancellationToken);
            dto.ReportedUserName = reported?.DisplayName ?? reported?.Username;
        }
        return dto;
    }

    private static string? NameOf(Dictionary<int, User?> users, int id)
        => users.TryGetValue(id, out var u) ? (u?.DisplayName ?? u?.Username) : null;

    private async Task<Expression<Func<Report, bool>>?> AdminPredicateAsync(AdminListQuery query, string? targetType, string? status, string[]? statuses, CancellationToken cancellationToken)
    {
        var predicate = AdminFilters.Common<Report>(query);
        if (!string.IsNullOrWhiteSpace(targetType))
        {
            var type = targetType.Trim();
            predicate = predicate.AndAlso(x => x.TargetType == type);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var wanted = status.Trim();
            predicate = predicate.AndAlso(x => x.Status == wanted);
        }
        var wantedSet = (statuses ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        if (wantedSet.Count > 0)
            predicate = predicate.AndAlso(x => x.Status != null && wantedSet.Contains(x.Status));
        if (query.SearchTerm is { } term)
        {
            var matched = (await _unitOfWork.Users.GetAllForAdminAsync(u => u.Username != null && u.Username.Contains(term), cancellationToken))
                .Select(u => u.Id).ToList();
            Expression<Func<Report, bool>> byReason = x => x.Reason != null && x.Reason.Contains(term);
            var byReasonOrPerson = byReason.OrElse(x => matched.Contains(x.ReporterId) || (x.ReportedUserId != null && matched.Contains(x.ReportedUserId.Value)));
            predicate = predicate.AndAlso(byReasonOrPerson);
        }
        return predicate;
    }

    public async Task<PagedResult<AdminReportDto>> GetAllForAdminPagedAsync(AdminListQuery query, string? targetType, string? status, string[]? statuses, CancellationToken cancellationToken = default)
    {
        var predicate = await AdminPredicateAsync(query, targetType, status, statuses, cancellationToken);
        var page = (await _unitOfWork.Reports.GetAllForAdminPagedAsync(query.SafePageNumber, query.SafePageSize, predicate, true, cancellationToken)).ToList();
        var total = await _unitOfWork.Reports.CountForAdminAsync(predicate, cancellationToken);

        var userIds = page.SelectMany(e => new[] { e.ReporterId, e.ReportedUserId ?? 0 }).Where(id => id > 0).Distinct().ToList();
        var users = userIds.Count == 0
            ? new Dictionary<int, User?>()
            : (await _unitOfWork.Users.GetAllForAdminAsync(u => userIds.Contains(u.Id), cancellationToken)).ToDictionary(u => u.Id, u => (User?)u);

        var dtos = page.Select(e =>
        {
            var dto = e.ToAdminDto();
            dto.ReporterName = NameOf(users, e.ReporterId);
            dto.ReportedUserName = e.ReportedUserId is { } rid ? NameOf(users, rid) : null;
            return dto;
        });
        return PagedResult<AdminReportDto>.Ok(dtos, total, query.SafePageNumber, query.SafePageSize);
    }

    public async Task<Result<AdminStatusCountsDto>> GetAdminStatusCountsAsync(AdminListQuery query, string? targetType, string? status, string[]? statuses, CancellationToken cancellationToken = default)
        => Result<AdminStatusCountsDto>.Ok(await _unitOfWork.Reports.GetAdminStatusCountsAsync(await AdminPredicateAsync(query, targetType, status, statuses, cancellationToken), cancellationToken));
}