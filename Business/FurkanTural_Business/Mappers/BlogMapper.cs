using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.Blog;

namespace FurkanTural_Business.Mappers;

public static class BlogMapper
{
    public static BlogDto ToDto(this Blog entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Content = entity.Content,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static AdminBlogDto ToAdminDto(this Blog entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Content = entity.Content,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static Blog ToEntity(this CreateBlogDto dto) => new()
    {
        Title = dto.Title,
        Content = dto.Content,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this Blog entity, UpdateBlogDto dto)
    {
        entity.Title = dto.Title;
        entity.Content = dto.Content;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}