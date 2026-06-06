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

    // ── Üye ──

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

    // ── Admin ──

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

    // ── Yardımcılar ──

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
}
