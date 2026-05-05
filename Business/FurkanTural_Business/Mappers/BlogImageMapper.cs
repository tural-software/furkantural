using FurkanTural_Domain.Entities;
using FurkanTural_Application.DTOs.BlogImage;

namespace FurkanTural_Business.Mappers;

public static class BlogImageMapper
{
    public static BlogImageDto ToDto(this BlogImage entity) => new()
    {
        Id = entity.Id,
        Url = entity.Url,
        AltText = entity.AltText,
        IsCover = entity.IsCover,
        BlogId = entity.BlogId
    };

    public static AdminBlogImageDto ToAdminDto(this BlogImage entity) => new()
    {
        Id = entity.Id,
        Url = entity.Url,
        AltText = entity.AltText,
        IsCover = entity.IsCover,
        BlogId = entity.BlogId,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        CreatedBy = entity.CreatedBy,
        UpdatedAt = entity.UpdatedAt,
        UpdatedBy = entity.UpdatedBy,
        DeletedAt = entity.DeletedAt
    };

    public static BlogImage ToEntity(this CreateBlogImageDto dto) => new()
    {
        Url = dto.Url,
        AltText = dto.AltText,
        IsCover = dto.IsCover,
        BlogId = dto.BlogId,
        CreatedBy = dto.CreatedBy
    };

    public static void UpdateEntity(this BlogImage entity, UpdateBlogImageDto dto)
    {
        entity.Url = dto.Url;
        entity.AltText = dto.AltText;
        entity.IsCover = dto.IsCover;
        entity.BlogId = dto.BlogId;
        entity.UpdatedBy = dto.UpdatedBy;
    }
}