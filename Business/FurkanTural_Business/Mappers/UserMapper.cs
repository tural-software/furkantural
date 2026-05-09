using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.User;

namespace FurkanTural_Business.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(this User entity) => new()
    {
        Id = entity.Id,
        Username = entity.Username,
        RoleId = entity.RoleId
    };

    public static AdminUserDto ToAdminDto(this User entity) => new()
    {
        Id = entity.Id,
        Username = entity.Username,
        RoleId = entity.RoleId,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static User ToEntity(this CreateUserDto dto) => new()
    {
        Username = dto.Username,
        // Password is set explicitly in UserService after encryption
        RoleId = dto.RoleId,
        CreatedBy = dto.CreatedBy
    };
}
