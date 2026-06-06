using FurkanTural_Application.DTOs.Report;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class ReportMapper
{
    public static AdminReportDto ToAdminDto(this Report entity) => new()
    {
        Id = entity.Id,
        ReporterId = entity.ReporterId,
        ReportedUserId = entity.ReportedUserId,
        TargetType = entity.TargetType,
        TargetId = entity.TargetId,
        Reason = entity.Reason,
        Status = entity.Status,
        AdminNote = entity.AdminNote,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };
}
