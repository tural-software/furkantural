using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.User;

namespace FurkanTural_Business.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(this User entity) => new()
    {
        Id = entity.Id,
        Username = entity.Username
    };

    public static User ToEntity(this CreateUserDto dto) => new()
    {
        Username = dto.Username,
        // Password is set explicitly in UserService after encryption
        CreatedBy = dto.CreatedBy
    };
}