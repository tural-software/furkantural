using FurkanTural_Application.DTOs.Tag;
using FurkanTural_Domain.Entities;

namespace FurkanTural_Business.Mappers;

public static class TagMapper
{
    public static TagDto ToDto(this Tag entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Slug = entity.Slug
    };

    public static AdminTagDto ToAdminDto(this Tag entity, int postCount = 0) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Slug = entity.Slug,
        PostCount = postCount,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt,
        DeletedBy = entity.DeletedBy
    };

    public static Tag ToEntity(this CreateTagDto dto) => new()
    {
        Name = dto.Name?.Trim()
    };

    public static void UpdateEntity(this Tag entity, UpdateTagDto dto)
    {
        entity.Name = dto.Name?.Trim();
    }
}
