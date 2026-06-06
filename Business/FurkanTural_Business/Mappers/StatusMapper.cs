using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Status;

namespace FurkanTural_Business.Mappers;

public static class StatusMapper
{
    public static StatusDto ToDto(this Status entity) => new()
    {
        Id = entity.Id,
        Group = entity.Group,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        Color = entity.Color,
        SortOrder = entity.SortOrder
    };

    public static AdminStatusDto ToAdminDto(this Status entity) => new()
    {
        Id = entity.Id,
        Group = entity.Group,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        Color = entity.Color,
        SortOrder = entity.SortOrder,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static Status ToEntity(this CreateStatusDto dto) => new()
    {
        Group = dto.Group,
        Code = dto.Code,
        Name = dto.Name,
        Description = dto.Description,
        Color = dto.Color,
        SortOrder = dto.SortOrder,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this Status entity, UpdateStatusDto dto)
    {
        entity.Group = dto.Group;
        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Color = dto.Color;
        entity.SortOrder = dto.SortOrder;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}
