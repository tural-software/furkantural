using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.User;

namespace FurkanTural_Business.Mappers;

/// <summary>ToEntity parolayı taşımaz. Kasıtlıdır: parola bu sınıfın göremeyeceği bir dönüşümden geçmesi gerektiği için <see cref="FurkanTural_Business.Services.Concrete.UserService"/> tarafından ayrıca yazılır. Buradan çıkan varlık, o adım atlanırsa parolasız kalır.</summary>
public static class UserMapper
{
    public static UserDto ToDto(this User entity) => new()
    {
        Id = entity.Id,
        Username = entity.Username,
        RoleId = entity.RoleId,
        Email = entity.Email,
        DisplayName = entity.DisplayName,
        AvatarUrl = entity.AvatarUrl
    };

    public static AdminUserDto ToAdminDto(this User entity) => new()
    {
        Id = entity.Id,
        Username = entity.Username,
        RoleId = entity.RoleId,
        Email = entity.Email,
        DisplayName = entity.DisplayName,
        AvatarUrl = entity.AvatarUrl,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };

    public static User ToEntity(this CreateUserDto dto) => new()
    {
        Username = dto.Username,
        RoleId = dto.RoleId,
        CreatedBy = dto.CreatedBy,
        Email = dto.Email,
        DisplayName = dto.DisplayName,
        AvatarUrl = dto.AvatarUrl
    };
}
