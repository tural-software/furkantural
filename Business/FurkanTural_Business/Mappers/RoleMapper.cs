using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Role;

namespace FurkanTural_Business.Mappers;

public static class RoleMapper
{
    public static RoleDto ToDto(this Role entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name
    };

    public static AdminRoleDto ToAdminDto(this Role entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static Role ToEntity(this CreateRoleDto dto) => new()
    {
        Name = dto.Name,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this Role entity, UpdateRoleDto dto)
    {
        entity.Name = dto.Name;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}
