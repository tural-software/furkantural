using FurkanTural_Application.DTOs.Call;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class CallLogMapper
{
    public static AdminCallLogDto ToAdminDto(this CallLog entity) => new()
    {
        Id = entity.Id,
        CallerId = entity.CallerId,
        CalleeId = entity.CalleeId,
        CallType = entity.CallType,
        Status = entity.Status,
        StartedAt = entity.StartedAt,
        AnsweredAt = entity.AnsweredAt,
        EndedAt = entity.EndedAt,
        DurationSeconds = entity.DurationSeconds,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };

    public static CallParticipantsDto ToParticipantsDto(this CallLog entity) => new()
    {
        Id = entity.Id,
        CallerId = entity.CallerId,
        CalleeId = entity.CalleeId,
        CallType = entity.CallType,
        Status = entity.Status
    };
}