using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Category;

namespace FurkanTural_Business.Mappers;

public static class CategoryMapper
{
    public static CategoryDto ToDto(this Category entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Color = entity.Color
    };

    public static AdminCategoryDto ToAdminDto(this Category entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Color = entity.Color,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static Category ToEntity(this CreateCategoryDto dto) => new()
    {
        Name = dto.Name?.Trim(),
        Color = dto.Color?.Trim(),
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this Category entity, UpdateCategoryDto dto)
    {
        entity.Name = dto.Name?.Trim();
        entity.Color = dto.Color?.Trim();
        entity.UpdatedBy = dto.UpdatedBy;
    }
}
