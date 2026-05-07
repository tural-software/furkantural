using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Experience;

namespace FurkanTural_Business.Mappers;

public static class ExperienceMapper
{
    public static ExperienceDto ToDto(this Experience entity) => new()
    {
        Id = entity.Id,
        Position = entity.Position,
        CompanyName = entity.CompanyName,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate
    };

    public static AdminExperienceDto ToAdminDto(this Experience entity) => new()
    {
        Id = entity.Id,
        Position = entity.Position,
        CompanyName = entity.CompanyName,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static Experience ToEntity(this CreateExperienceDto dto) => new()
    {
        Position = dto.Position,
        CompanyName = dto.CompanyName,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this Experience entity, UpdateExperienceDto dto)
    {
        entity.Position = dto.Position;
        entity.CompanyName = dto.CompanyName;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}
